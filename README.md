# Réveil musical

Service qui réveille chaque utilisateur avec un morceau choisi selon le jour et la météo, puis le prévient sur son canal préféré — sans jamais rester silencieux.

## Lancer

```bash
dotnet run --project src/ReveilMusical.Api --urls http://localhost:5080
curl -X POST http://localhost:5080/wakeups -H 'Content-Type: application/json' \
     -d '{"userId":"42","day":"LUNDI","weather":"PLUIE"}'
```

| Code | Signification |
|---|---|
| 200 | Réveil envoyé ; `degraded` et `reasons` indiquent un éventuel mode dégradé |
| 400 | Requête invalide (`weather` ∈ SOLEIL, PLUIE, NEIGE, NUAGEUX ; `day` ∈ LUNDI…DIMANCHE ; insensible à la casse, les entiers sont refusés) |
| 404 | Utilisateur inconnu |
| 503 | Aucun canal n'a pu délivrer : l'ordonnanceur peut réessayer |

Utilisateurs de démonstration : `42` (grille jour×météo, push), `7` (morceau de secours, SMS), `13` (email préféré mais absent → repli sur push).

## Tests et couverture

```bash
dotnet test
scripts/coverage.sh        # 99,5 % des lignes (hors Program.cs)
scripts/audit-dependencies.sh
```

`global.json` sélectionne Microsoft.Testing.Platform comme exécuteur de tests (requis par xunit.v3 avec le SDK 10) ; la couverture passe par `coverlet.MTP`.

## Architecture

```
Domain  ←  Application  ←──  Api (composition root)
   ↑                          │
   └──────  Infrastructure  ←─┘
```

- **Domain** : modèle (`UserProfile`, `Track`, `WakeUpMessage`) et ports (`IUserProfileProvider`, `ITrackResolver`, `IWakeUpNotifier`).
- **Application** : `WakeUpService`, unique cas d'usage ; ne connaît que les ports.
- **Infrastructure** : adapters iTunes / MusicBrainz / catalogue local, mocks email / SMS / push, profils en mémoire. Tout est `internal` sauf `AddInfrastructure`.
- **Api** : `POST /wakeups`, traduction JSON ↔ domaine, ProblemDetails.

Les références de projets rendent l'isolation vérifiable par le compilateur ; `ArchitectureTests` vérifie en plus que Domain et Application ne référencent ni HTTP, ni JSON, ni l'infrastructure.

### Exigences métier → choix techniques

| Exigence | Réponse |
|---|---|
| Changer de fournisseur musical rapidement | `Music:Providers` dans `appsettings.json` : ordre et présence des fournisseurs sans toucher au code. Nouveau fournisseur = un adapter (sous-classe de `HttpMusicProviderBase<T>`), une valeur `MusicProviderKind`, une propriété `RemoteProviderOptions` + son bloc `appsettings`, une ligne de DI ; l'ordre se règle ensuite dans `Music:Providers`. |
| Nouveaux canaux de notification | Un adapter `INotificationChannel` (qui déclare son nom, ex. `"WhatsApp"`), une ligne de DI et sa place dans `Notifications:FallbackOrder` : **aucune modification du Domain**. Le métier ne manipule qu'un `ChannelType` opaque (un nom) et un `UserContact` qui associe à chaque canal une adresse ; c'est l'adapter qui sait quoi faire de son adresse. Un nom de canal inconnu dans la configuration fait échouer le démarrage. |
| Aucun silence | Chaque frontière a un délai maximal (musique `Music:*:Timeout` 3 s, profils `Profiles:Timeout` 2 s, chaque canal `Notifications:ChannelTimeout` 2 s)  : une lenteur est traitée comme une panne, y compris face à un adapter bloquant ou qui ignore l'annulation (`Task.Run` + `WaitAsync`). Repli musical jusqu'au catalogue local (infaillible), repli de canal, service de profils indisponible → dernier profil connu (mis en cache à chaque consultation réussie, conservé 7 jours, propre à chaque instance) et signalé `StaleProfileUsed` ; si le profil n'a jamais été vu, le réveil choisit tout de même un morceau dans le catalogue local mais aucun contact n'est connu : l'API répond alors 503 explicitement pour que l'ordonnanceur réessaie, jamais un 200 silencieux. |
| Quotas des API | Cache (24 h) devant un limiteur par fournisseur (iTunes 20/min, MusicBrainz 1/s) ; quota atteint → fournisseur suivant, sans attendre. |
| Aucun composant non vérifié | Tableau ci-dessous, `scripts/audit-dependencies.sh`, `NuGetAuditMode=all` + `TreatWarningsAsErrors` : une vulnérabilité connue casse le build. |

### Design patterns

| Pattern | Où | Pourquoi |
|---|---|---|
| Adapter | `ITunesMusicProvider`, `MusicBrainzMusicProvider`, `EmailChannel`, `SmsChannel`, `PushChannel` | Traduire des contrats externes hétérogènes (`trackViewUrl`, `artist-credit`, `Send(phone, text) : bool`, `PushAsync(payload)`) vers le modèle métier |
| Strategy | `IMusicProvider`, `INotificationChannel` sélectionnés par configuration | Interchangeabilité à chaud via la config |
| Composite | `FallbackTrackResolver`, `FallbackNotifier` | Un ensemble ordonné de fournisseurs se présente au métier comme un seul port |
| Decorator | `CachingMusicProvider` ∘ `RateLimitedMusicProvider` ∘ adapter ; `LastKnownUserProfileProvider` ∘ `InMemoryUserProfileProvider` | Ajouter cache, quota et dernier profil connu sans modifier les adapters |
| Template Method | `HttpMusicProviderBase<T>` | Envoi, timeout et gestion d'erreurs communs ; chaque API ne fournit que l'URL et le mapping |
| Singleton (durée de vie DI) | limiteurs de débit, cache, catalogue local, mocks | Un quota n'a de sens que partagé par tout le processus ; pas de `static Instance`, donc testable |
| Special Case | `LocalMusicProvider` | Renvoie toujours un morceau, y compris pour une météo inconnue |
| Result | `WakeUpResult`, `NotificationResult`, `ResolvedTrack` | Le mode dégradé est une donnée explicite, pas une exception avalée |

### Règle « aucun `new` »

Aucun service n'est instancié par `new` dans `src/` : la DI et `ActivatorUtilities.CreateInstance` (décorateurs, composites, limiteurs) s'en chargent. Restent instanciés directement les **valeurs** — records du domaine, DTO, options, `Uri` — qui sont des données et non des dépendances. Les tests, qui jouent le rôle de composition root, instancient librement.

## Dépendances

Aucune dépendance système cachée : les noms français des jours sont écrits en dur plutôt que lus via ICU (`CultureInfo("fr-FR")`), et les tests tournent en `InvariantGlobalization` pour le garantir — l'API fonctionne donc aussi sur une image sans libicu (Alpine, chiseled).

Plateforme (vérifiée le 2026-10-08 sur le releases-index de dotnet.microsoft.com) ; `global.json` exige un SDK .NET 10 (`10.0.100` minimum, `rollForward: latestFeature`) :

| Composant | Installé | Dernière stable | Licence | Support |
|---|---|---|---|---|
| .NET SDK | 10.0.401 | 10.0.401 | MIT | LTS (fin de support 2028-11-14) |
| Microsoft.NETCore.App (runtime) | 10.0.12 | 10.0.12 | MIT | LTS |
| Microsoft.AspNetCore.App (runtime) | 10.0.12 | 10.0.12 | MIT | LTS |

Toutes les versions sont centralisées dans `Directory.Packages.props`. Tableau généré le 08/10/2026 par `scripts/audit-dependencies.sh` depuis nuget.org :

| Paquet | Installée | Dernière stable | Publiée le | Licence | Statut |
|---|---|---|---|---|---|
| coverlet.MTP | 10.1.0 | 10.1.0 | 2026-09-27 | MIT | à jour |
| dotnet-reportgenerator-globaltool | 5.5.11 | 5.5.11 | 2026-07-27 | Apache-2.0 | à jour |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Caching.Memory | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Configuration | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Http | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Logging.Abstractions | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Options | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| NSubstitute | 6.2.0 | 6.2.0 | 2026-08-11 | BSD-3-Clause | à jour |
| Shouldly | 4.3.0 | 4.3.0 | 2025-01-23 | BSD-3-Clause | à jour |
| System.Threading.RateLimiting | 10.0.12 | 10.0.12 | 2026-09-08 | MIT | à jour |
| WireMock.Net | 2.19.0 | 2.19.0 | 2026-10-07 | Apache-2.0 | à jour |
| xunit.v3 | 4.0.1 | 4.0.1 | 2026-09-12 | Apache-2.0 | à jour |

Les paquets `Microsoft.Extensions.*` et `System.Threading.RateLimiting` (dépendance d'exécution : ses types ne font pas partie du framework de base `Microsoft.NETCore.App`) sont sous licence MIT, maintenus par Microsoft et alignés sur .NET 10 (LTS). ASP.NET Core vient du shared framework `Microsoft.AspNetCore.App` ; `Microsoft.AspNetCore.Mvc.Testing` est en revanche un paquet NuGet **de test uniquement**. xUnit, NSubstitute, Shouldly, coverlet, WireMock.Net et ReportGenerator sont des composants tiers, tous de test ou d'outillage.

Fraîcheur : Shouldly 4.3.0 date de 2025-01 (stable mais cadence de publication faible, acceptable pour une bibliothèque d'assertions de test) ; WireMock.Net est publié très fréquemment (dernière version 2026-10-07).

**Licences transitives.** La seconde section de `scripts/audit-dependencies.sh` parcourt les 187 paquets distincts (directs et transitifs, tous projets) résolus par `dotnet list package --include-transitive` et lit la licence de chaque `.nuspec` : MIT 151, Apache-2.0 22, BSD-3-Clause 2, BSD-2-Clause 1, MS-PL 1, et 10 sans expression SPDX reconnue :

| Paquet | Déclaré | Licence réelle (vérifiée manuellement) | Présent dans |
|---|---|---|---|
| Fare 2.2.1, SimMetrics.Net 1.0.5 | licenseUrl seule | MIT (fichier LICENSE du dépôt) | tests (via WireMock.Net) |
| JmesPath.Net.Parser 1.1.0 | licenseUrl seule | Apache-2.0 | tests (via WireMock.Net) |
| Microsoft.AspNetCore.Http, .Http.Abstractions, .Http.Features, .WebUtilities, Microsoft.Net.Http.Headers (2.3.x) | licenseUrl seule | Apache-2.0 (lu pour Http 2.3.9 ; les autres pointent le même dépôt AspNetCore) | tests (via WireMock.Net) |
| Json.More.Net 3.0.1, JsonPath.Net 3.0.2 | `OSMFEULA.txt` | MIT + Open Source Maintenance Fee : redevance due seulement pour un usage commercial générateur d'au moins 10 000 USD de revenu annuel | tests (via WireMock.Net) |

Aucun paquet de ces 10 n'est référencé par les projets de `src/` (vérifié avec `dotnet list package --include-transitive` sur chacun) : ils ne sont pas livrés avec le produit. La seule licence à réciprocité est la MS-PL de `xpath2` 1.1.5 (copyleft faible, au niveau du fichier), tirée par WireMock.Net pour les tests uniquement : elle ne s'applique qu'à ses propres sources et ne touche pas le produit livré. Aucune GPL/AGPL n'apparaît ; la licence réelle des 8 paquets « licenseUrl seule » a été lue à la main, le script ne peut pas la lire. Le contrat OSMF des deux paquets `json-everything` est à connaître pour un usage commercial de l'environnement de test, mais ne concerne pas le produit livré.

### Composants écartés

| Composant | Raison |
|---|---|
| FluentAssertions ≥ 8 | Licence commerciale payante depuis 2025 (la v7 Apache-2.0 serait une version figée) → Shouldly |
| Moq | Incident SponsorLink (2023, retiré ensuite) : collecte de données à la compilation ; la confiance est entamée → NSubstitute |
| MediatR, AutoMapper | Passés sous licence commerciale en 2025 ; un seul cas d'usage et un mapping trivial ne les justifient pas |
| Polly / Microsoft.Extensions.Http.Resilience | Timeout `HttpClient` + `System.Threading.RateLimiting` suffisent ; les décorateurs restent lisibles |
| Scrutor | Superflu : `ActivatorUtilities` couvre la décoration |
| NetArchTest | Dépendance superflue pour un test par réflexion de dix lignes |

## APIs externes

| API | Conditions respectées |
|---|---|
| iTunes Search API | Gratuite, sans clé, ≈ 20 requêtes/min : limiteur 20/min + cache 24 h. Le lien de retour vers Apple (`trackViewUrl`) est conservé sous forme neutre `ListenUrl` (attendu par les conditions d'utilisation). |
| MusicBrainz | `User-Agent` identifiable obligatoire (`Music:MusicBrainz:UserAgent`, validé au démarrage ; le contact donné est l'URL du dépôt), 1 requête/s en moyenne : limiteur 1/s. Les données utilisées (titre, artiste) font partie des données de base, publiées en CC0. |
