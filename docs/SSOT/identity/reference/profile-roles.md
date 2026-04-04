# ProfileRoleNames — green-ai

> **Autoritet:** Denne fil er SSOT for alle ProfileRole-capabilities i green-ai.
> **Kilde:** Analyseret fra sms-service med domæne-kategorisering — ikke kopi.
> **Opdateres:** Når ny ProfileRole tilføjes til `ProfileRoles`-tabellen + `ProfileRoleNames.cs`.

---

## Hvad er ProfileRole?

En ProfileRole er et **operationelt capability-flag per profil** — ikke per bruger.

```
DoesProfileHaveRole(profileId, ProfileRoleNames.CanSendByEboks)
→ true/false — gater adgang til en feature
```

**Nøgleregel:** Customer-membership giver IKKE automatisk ProfileRole-adgang. Roller tildeles eksplicit per profil via `ProfileRoleMappings`. `ProfileRoleGroup` er kun en oprettelsestidspunkts-template — ændring af gruppe opdaterer IKKE eksisterende profiler.

---

## Kategorier

### Kanal-adgang (hvilke kanaler profilen må sende via)
| Rolle | Beskrivelse |
|-------|-------------|
| `CanSendByWeb` | Afsendelse via web-interface (standard) |
| `CanSendByWebInternal` | Afsendelse via intern web-portal |
| `CanSendByVoice` | Afsendelse som voice-opkald (Infobip) |
| `CanSendByEboks` | Afsendelse via eBoks digital post |
| `CanSendViaEmail2sms` | Email-til-SMS gateway |
| `CanSendByMap` | Afsendelse via kortbaseret selektion |
| `CanSendByAddressSelection` | Afsendelse via adresse-selektion |
| `CanSendByMunicipalitySelection` | Afsendelse via kommuneselektion |
| `CanSendByGroupedPosList` | Afsendelse via grupperet positivliste |
| `CanSendByResend` | Gensendelse af tidligere broadcast |
| `CanSendByOnlyStdReceivers` | Kun standardmodtagere — ingen fri selektion |
| `SendToVarsleMeg` | Afsendelse til Varslomeg (NO) |

### Modtager-typer og -udvidelse
| Rolle | Beskrivelse |
|-------|-------------|
| `CanSelectStdReceivers` | Vælg standardmodtagere |
| `StdReceiverExtended` | Udvidet standardmodtager-funktionalitet |
| `DistributeToStdReceiverGroups` | Distribuér til standardmodtager-grupper |
| `CanSendToSubscriptionNumbers` | Afsendelse til abonnentregistrerede numre |
| `CanSendToCriticalAddresses` | Afsendelse til kritiske adresser (nødberedskab) |
| `AlwaysCanReceiveSmsReply` | Profil modtager altid SMS-svar |
| `SmsConversations` | Tovejs SMS-samtaler aktivt |

### Adgangsbegrænsninger og -overrides
| Rolle | Beskrivelse |
|-------|-------------|
| `HaveNoSendRestrictions` | **Ingen afsendelsesrestriktioner** — bypasser geografisk adgangskontrol |
| `OverruleBlockedNumber` | Kan sende til blokerede numre |
| `RobinsonCheck` | Robinson-register kontrolleres ved afsendelse |
| `DontLookUpNumbers` | Spring opslag over — brug nummer som-er |
| `DuplicateCheckWithKvhx` | Duplikat-tjek via KVHX |
| `NameMatch` | Navnematch aktiveret ved opslag |
| `NorwayKRRLookup` | Norsk KRR-opslag (reservationsregister) |
| `Norway1881Lookup` | Norsk 1881-opslag |
| `MapFindProperties` | Kortbaseret ejendomssøgning |

### Publicering og sociale medier
| Rolle | Beskrivelse |
|-------|-------------|
| `CanPostOnFacebook` | Kan publicere på Facebook |
| `CanTweetOnTwitter` | Kan tweete på Twitter |
| `AlwaysPostOnFacebook` | Publicerer altid på Facebook |
| `AlwaysPostOnTwitter` | Publicerer altid på Twitter |
| `AlwaysPostOnWeb` | Publicerer altid på websiden |
| `AlwaysPostOnVoice` | Sender altid som voice |
| `AlwaysPostOnInternal` | Publicerer altid internt |
| `CriticalStatusWeb` | Kritisk status vises på websiden |

### Benchmark og statistik
| Rolle | Beskrivelse |
|-------|-------------|
| `HasBenchmark` | Adgang til benchmark-modul |
| `AlwaysBenchmark` | Benchmark altid aktivt |

### Afsendelsesadfærd og defaults
| Rolle | Beskrivelse |
|-------|-------------|
| `AlwaysOwner` | Er altid ejer af broadcasts |
| `AlwaysEboks` | Sender altid via eBoks |
| `AlwaysDelayed` | Broadcasts altid scheduleret (aldrig straks) |
| `NotAlwaysSmsText` | SMS-tekst er ikke obligatorisk |
| `DontSendEmail` | Undertrykker email-afsendelse |
| `BroadcastNoReciept` | Ingen leveringskvittering |
| `HighPrioritySender` | Høj prioritet i afsendelseskø |
| `QuickResponse` | QuickResponse-funktion aktiveret |
| `Nynorsk` | Brug nynorsk sprog (NO) |
| `SendVoiceMessagesToMobileStdReceivers` | Voice til mobil standardmodtagere |

### Infoportal og webmeddelelser
| Rolle | Beskrivelse |
|-------|-------------|
| `HasInfoPortal` | Adgang til infoportal |
| `HideOnTheStatusPageForSuperAdmins` | Skjul profil på status-side for SuperAdmins |
| `UseMunicipalityPolList` | Brug kommunens pol-liste |
| `CanUploadStreetList` | Upload gadelist |
| `CanSpecifyLookup` | Specificér opslags-parametre |
| `CanSelectLookupBusinessOrPrivate` | Vælg erhverv eller privat opslag |

### Integrationer (tredjepart)
| Rolle | Beskrivelse |
|-------|-------------|
| `TrimbleIntegration` | Trimble WSDL-integration aktiveret |
| `KamstrupREADy` | Kamstrup READy integration |
| `Statstidende` | Adgang til Statstidende-publicering |
| `AdProvisioning` | AD-provisionering |
| `CitizenDialogue` | CitizenDialogue-feature (aktiv sprint 2026) |

### Vejrvarsler
| Rolle | Beskrivelse |
|-------|-------------|
| `HasWeatherWarning` | Adgang til vejrvarslingsmodul |

---

## green-ai implementeringsregler

```csharp
// ✅ Korrekt — strongly typed navn, ingen magic strings
await _permissions.DoesProfileHaveRoleAsync(profileId, ProfileRoleNames.CanSendByEboks);

// ❌ Forkert — aldrig raw string
await _permissions.DoesProfileHaveRoleAsync(profileId, "CanSendByEboks");
```

```
✅ Roller tildeles eksplicit via ProfileRoleMappings
✅ ProfileRoleGroup er kun template ved oprettelse
✅ Alle 60 værdier har matchende DB-entry i ProfileRoles-tabellen
❌ Aldrig forudsæt at CustomerMembership giver ProfileRole
❌ ProfileRoleGroup-ændring opdaterer IKKE eksisterende profilers roller
```

---

## C# enum (green-ai)

```csharp
public enum ProfileRoleNames
{
    Undefined = 0,
    // Kanal
    CanSendByGroupedPosList, CanSendByResend, CanSendByWebInternal, CanSendByMap,
    CanSendByWeb, CanSendByVoice, CanSendByEboks, CanSendViaEmail2sms,
    CanSendByAddressSelection, CanSendByMunicipalitySelection, CanSendByOnlyStdReceivers,
    SendToVarsleMeg,
    // Modtagere
    CanSelectStdReceivers, StdReceiverExtended, DistributeToStdReceiverGroups,
    CanSendToSubscriptionNumbers, CanSendToCriticalAddresses, AlwaysCanReceiveSmsReply,
    SmsConversations,
    // Adgang
    HaveNoSendRestrictions, OverruleBlockedNumber, RobinsonCheck, DontLookUpNumbers,
    DuplicateCheckWithKvhx, NameMatch, NorwayKRRLookup, Norway1881Lookup,
    MapFindProperties,
    // Publicering
    CanPostOnFacebook, CanTweetOnTwitter, AlwaysPostOnFacebook, AlwaysPostOnTwitter,
    AlwaysPostOnWeb, AlwaysPostOnVoice, AlwaysPostOnInternal, CriticalStatusWeb,
    // Benchmark
    HasBenchmark, AlwaysBenchmark,
    // Adfærd
    AlwaysOwner, AlwaysEboks, AlwaysDelayed, NotAlwaysSmsText, DontSendEmail,
    BroadcastNoReciept, HighPrioritySender, QuickResponse, Nynorsk,
    SendVoiceMessagesToMobileStdReceivers,
    // Portal
    HasInfoPortal, HideOnTheStatusPageForSuperAdmins, UseMunicipalityPolList,
    CanUploadStreetList, CanSpecifyLookup, CanSelectLookupBusinessOrPrivate,
    // Integrationer
    TrimbleIntegration, KamstrupREADy, Statstidende, AdProvisioning, CitizenDialogue,
    // Vejr
    HasWeatherWarning,
}
```
