# Réveil musical

Service qui réveille chaque utilisateur avec un morceau choisi selon le jour et la météo, puis le prévient sur son canal préféré — sans jamais rester silencieux.

## Lancer

```bash
dotnet run --project src/ReveilMusical.Api --urls http://localhost:5080
curl -X POST http://localhost:5080/wakeups -H 'Content-Type: application/json' \
     -d '{"userId":"42","day":"Monday","weather":"PLUIE"}'
```

| Code | Signification |
|---|---|
| 200 | Réveil envoyé ; `degraded` et `reasons` indiquent un éventuel mode dégradé |
| 400 | Requête invalide (`weather` ∈ SOLEIL, PLUIE, NEIGE, NUAGEUX ; `day` ∈ Monday…Sunday) |
| 404 | Utilisateur inconnu |
| 503 | Aucun canal n'a pu délivrer : l'ordonnanceur peut réessayer |

Utilisateurs de démonstration : `42` (grille jour×météo, push), `7` (morceau de secours, SMS), `13` (email préféré mais absent → repli sur push).

## Tests et couverture

```bash
dotnet test
scripts/coverage.sh        # couverture de lignes : 99,4 %
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
| Changer de fournisseur musical rapidement | `Music:Providers` dans `appsettings.json` : ordre et présence des fournisseurs sans toucher au code. Nouveau fournisseur = un adapter + une ligne de DI. |
| Nouveaux canaux de notification | Un adapter `INotificationChannel` + une ligne de DI ; ordre de repli dans `Notifications:FallbackOrder`. |
| Aucun silence | Repli musical jusqu'au catalogue local (infaillible), repli de canal, profil indisponible → mode dégradé, 503 explicite si rien n'a pu partir. |
| Quotas des API | Cache (24 h) devant un limiteur par fournisseur (iTunes 20/min, MusicBrainz 1/s) ; quota atteint → fournisseur suivant, sans attendre. |
| Aucun composant non vérifié | Tableau ci-dessous, `scripts/audit-dependencies.sh`, `NuGetAuditMode=all` + `TreatWarningsAsErrors` : une vulnérabilité connue casse le build. |

### Design patterns

| Pattern | Où | Pourquoi |
|---|---|---|
| Adapter | `ITunesMusicProvider`, `MusicBrainzMusicProvider`, `EmailChannel`, `SmsChannel`, `PushChannel` | Traduire des contrats externes hétérogènes (`trackViewUrl`, `artist-credit`, `Send(phone, text) : bool`, `PushAsync(payload)`) vers le modèle métier |
| Strategy | `IMusicProvider`, `INotificationChannel` sélectionnés par configuration | Interchangeabilité à chaud via la config |
| Composite | `FallbackTrackResolver`, `FallbackNotifier` | Un ensemble ordonné de fournisseurs se présente au métier comme un seul port |
| Decorator | `CachingMusicProvider` ∘ `RateLimitedMusicProvider` ∘ adapter | Ajouter cache et quota sans modifier les adapters |
| Template Method | `HttpMusicProviderBase<T>` | Envoi, timeout et gestion d'erreurs communs ; chaque API ne fournit que l'URL et le mapping |
| Singleton (durée de vie DI) | limiteurs de débit, cache, catalogue local, mocks | Un quota n'a de sens que partagé par tout le processus ; pas de `static Instance`, donc testable |
| Special Case | `LocalMusicProvider` | Renvoie toujours un morceau, y compris pour une météo inconnue |
| Result | `WakeUpResult`, `NotificationResult`, `ResolvedTrack` | Le mode dégradé est une donnée explicite, pas une exception avalée |

### Règle « aucun `new` »

Aucun service n'est instancié par `new` dans `src/` : la DI et `ActivatorUtilities.CreateInstance` (décorateurs, composites, limiteurs) s'en chargent. Restent instanciés directement les **valeurs** — records du domaine, DTO, options, `Uri` — qui sont des données et non des dépendances. Les tests, qui jouent le rôle de composition root, instancient librement.

## Dépendances

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

Les paquets `Microsoft.Extensions.*` et ASP.NET Core (shared framework `Microsoft.AspNetCore.App`) sont sous licence MIT, maintenus par Microsoft et alignés sur .NET 10 (LTS). `System.Threading.RateLimiting` (Microsoft, MIT) est une dépendance d'exécution : ses types ne font pas partie du framework de base `Microsoft.NETCore.App`. Seuls xUnit, NSubstitute, Shouldly, coverlet, WireMock.Net et ReportGenerator sont des composants tiers, tous **uniquement de test** et sous licence permissive (Apache-2.0, BSD-3-Clause, MIT) : aucune obligation copyleft ne s'applique au produit livré. WireMock.Net tire des dépendances transitives ; elles sont couvertes par `--include-transitive` et par l'audit NuGet du build.

### Composants écartés

| Composant | Raison |
|---|---|
| FluentAssertions ≥ 8 | Licence commerciale payante depuis 2025 (la v7 Apache-2.0 serait une version figée) → Shouldly |
| Moq | Incident SponsorLink (2023) : collecte de données à la compilation → NSubstitute |
| MediatR, AutoMapper | Passés sous licence commerciale en 2025 ; un seul cas d'usage et un mapping trivial ne les justifient pas |
| Polly / Microsoft.Extensions.Http.Resilience | Timeout `HttpClient` + `System.Threading.RateLimiting` suffisent ; les décorateurs restent lisibles |
| Scrutor | `ActivatorUtilities` couvre la décoration sans dépendance |
| NetArchTest | Peu maintenu ; un test par réflexion de dix lignes suffit |

## APIs externes

| API | Conditions respectées |
|---|---|
| iTunes Search API | Gratuite, sans clé, ≈ 20 requêtes/min : limiteur 20/min + cache 24 h. Le lien `trackViewUrl` (renvoi vers Apple, exigé par les conditions d'Apple) est conservé sous forme neutre `ListenUrl`. |
| MusicBrainz | `User-Agent` identifiable obligatoire (`Music:MusicBrainz:UserAgent`, validé au démarrage), 1 requête/s en moyenne : limiteur 1/s. Les données utilisées (titre, artiste) font partie des données de base, publiées en CC0. |
