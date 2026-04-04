# AppSetting Enum — green-ai

> **Autoritet:** Denne fil er SSOT for alle `AppSetting`-nøgler i green-ai.
> **Kilde:** Udtrykt fra sms-service som struktureret reference — ikke kopi.
> **Opdateres:** Når ny nøgle tilføjes til `ApplicationSettings`-tabellen.

---

## Hvad er AppSetting?

`AppSetting` er en strongly typed enum der identificerer konfigurationsnøgler i `ApplicationSettings`-tabellen. Designet er:

- Én tabel, alle nøgler (`ApplicationSettingTypeId` = enum int-value)
- Full-load cache ved opstart, nøgle `Sms.applicationsettings` — invalideres ved `Save()`
- Ny nøgle kræver: (1) nyt enum-felt, (2) `CreateDefault()` kaldt ved opstart

**Brug:**
```csharp
var key = _settings.Get(AppSetting.SendGridAPIKey, defaultValue: "");
_settings.Save(setting); // → cache-invalidering automatisk
```

---

## Kategorier og nøgler

### Gateway / SMS (DK, SE, FI, NO)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `GatewayAPISingleToken` | 33 | GatewayAPI token — DK (primær) |
| `GatewayAPISingleTokenSE` | 176 | GatewayAPI token — Sverige |
| `GatewayAPISingleTokenFI` | 177 | GatewayAPI token — Finland |
| `GatewayAPISingleTokenNO` | 187 | GatewayAPI token — Norge |
| `GatewayAPISingleTokenHighPrio` | 185 | GatewayAPI token — høj prioritet (time-critical SMS) |
| `GatewayAPIUrl` | 183 | GatewayAPI base URL |

### Kill-switches (system-wide)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `DisableAllBroadcasts` | 184 | **Systemglobal kill-switch** — stopper al broadcast-afsendelse |
| `DisableAllSystemMessages` | 206 | **Systemglobal kill-switch** — stopper alle systembeskeder |
| `DisableMessageBackgroundService` | 202 | Stopper SMS/MQ baggrundstjeneste |

### Email (SendGrid)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `SendGridAPIKey` | 130 | SendGrid API-nøgle til email-afsendelse |
| `SendGridHost` | 229 | SendGrid SMTP-host (alternativ afsendelse) |

### Voice (Infobip)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `VoiceGateway` | 142 | Voice gateway URL |
| `InfobipApiKey` | 143 | Infobip API-nøgle |
| `InfobipApiEndpoint` | 144 | Infobip API endpoint |

### Azure Batch
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `BatchAppProgramVersion` | 100 | Aktuel batch-applikationsversion |
| `BatchAccountName` | 101 | Azure Batch konto-navn |
| `BatchAccountKey` | 102 | Azure Batch konto-nøgle |
| `BatchAccountUrl` | 103 | Azure Batch konto-URL |
| `BatchPoolName` | 104 | Azure Batch pool-navn |
| `BatchAppCommandLine` | 105 | Azure Batch kommandolinje |

### Logging
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `RequestLogLevel` | 160 | HTTP request/response logging: `off` / `Error` / `All` |

### Adresseopslag — Norge
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `NorwegianPhoneLookupApiKey` | 188 | Norsk opslags-API nøgle (basis) |
| `NorwegianPhoneExtendedApiKey` | 189 | Norsk opslags-API nøgle (udvidet) |
| `NorwegianApi1881Url` | 190 | 1881 API URL |
| `NorwegianPublicContactsAccessKey` | 207 | KRR access key |
| `NorwegianPublicContactsAccessToken` | 208 | KRR access token |
| `NorwegianPublicContactsApiUrl` | 209 | KRR API URL |
| `NorwegianSoapEndpointAddress` | 223 | Norsk SOAP endpoint |
| `NorwegianSoapUserName` | 224 | Norsk SOAP brugernavn |
| `NorwegianSoapPassword` | 225 | Norsk SOAP password |
| `LastNorwegianAddressChangeId` | 228 | Sidst processerede norsk adresseændrings-ID |

### Adresseopslag — Sverige (Lantmäteriet)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `LantmäterietKey` | 220 | Lantmäteriet API-nøgle |
| `LantmäterietSecret` | 221 | Lantmäteriet API-hemmelighed |
| `LantmäterietUserName` | 260 | Lantmäteriet brugernavn |
| `LantmäterietUserPassword` | 261 | Lantmäteriet password |
| `LantmäterietMapUserName` | 239 | Lantmäteriet kortbrugernavn |
| `LantmäterietMapPassword` | 240 | Lantmäteriet kortpassword |

### Adresseopslag — Danmark
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `OwnerFilePattern` | 152 | Ejerskabsfil-mønster |
| `OwnerDatafordelerCertFileName` | 153 | Datafordeler certifikat-filnavn |
| `OwnerDatafordelerHost` | 154 | Datafordeler host |
| `OwnerDatafordelerUserName` | 155 | Datafordeler brugernavn |
| `AddressBfeExclusion` | 226 | BFE-ekskluderingsliste |
| `EjerfortegnelseCertificateThumbprint` | 169 | Ejerfortegnelse certifikat-thumbprint |

### Robinson-register (DK)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `RobinsonFTPhost` | 90 | Robinson FTP-host |
| `RobinsonFTPuser` | 91 | Robinson FTP-bruger |
| `RobinsonSFTPpassword` | 180 | Robinson SFTP-password |
| `RobinsonFTPlatestFilename` | 94 | Seneste Robinson-filnavn |

### eBoks
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `EboksCvrApiCertificateThumbprint` | 167 | eBoks CVR API certifikat |

### Statstidende
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `StatstidendeCertificateThumbprint` | 170 | Statstidende certifikat |

### Vejrvarsler
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `WeatherWarningHost` | 149 | Vejrvarslings-host |
| `WeatherWarningFilename` | 150 | Vejrvarslings-filnavn |
| `WeatherWarningMD5Filename` | 151 | Vejrvarslings MD5-fil |
| `WeatherWarningApiKey` | 241 | Vejrvarslings API-nøgle |

### Strex (NordiBet-betaling)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `StrexApiUrl` | 270 | Strex API URL |
| `EnableStrex` | 271 | Global Strex-aktivering |
| `StrexApiKeyDK` | 275 | Strex API-nøgle DK |
| `StrexApiKeyFI` | 276 | Strex API-nøgle FI |
| `StrexApiKeyNO` | 277 | Strex API-nøgle NO |
| `StrexApiKeySE` | 278 | Strex API-nøgle SE |
| `EnableStrexDK` | 300 | Strex-aktivering per land — DK |
| `EnableStrexSE` | 301 | Strex-aktivering per land — SE |
| `EnableStrexFI` | 302 | Strex-aktivering per land — FI |
| `EnableStrexNO` | 303 | Strex-aktivering per land — NO |

### Maskinporten / KoFuVi (Norge)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `MaskinportenLoginCertificateThumbprint` | 234 | Maskinporten certifikat |
| `MaskinportenApiUrl` | 236 | Maskinporten API URL |
| `KoFuViCertificateThumbprint` | 235 | KoFuVi certifikat |
| `KoFuViApiUrl` | 237 | KoFuVi API URL |
| `KrrApiUrl` | 238 | KRR API URL |

### Facebook / Twitter / Sociale medier
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `FacebookAppId` | 145 | Facebook app ID |
| `FacebookAppSecret` | 146 | Facebook app-hemmelighed |
| `TwitterConsumerKey` | 147 | Twitter consumer key |
| `TwitterConsumerSecret` | 148 | Twitter consumer secret |

### Tony / Fact24 (tredjeparts crisisplatform)
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `TonyStatusStreamUrl` | 242 | Tony status stream URL |
| `TonyCallsAPIUrl` | 243 | Tony calls API URL |
| `Fact24AuthUrl` | 244 | Fact24 auth URL |
| `Fact24AuthClientId` | 245 | Fact24 client ID |
| `Fact24AuthClientSecret` | 246 | Fact24 client secret |

### Diverse
| Enum-nøgle | ID | Beskrivelse |
|---|---|---|
| `TeleAdressUserID` | 120 | TeleAdress bruger-ID |
| `TeleAdressPassword` | 121 | TeleAdress password |
| `DropboxClientKey` | 168 | Dropbox client-nøgle |
| `ProfinderApiKey` | 171 | Profinder API-nøgle |
| `QuickResponseRandomChars` | 210 | QR-kode tilfældige tegn |
| `TrimbleIpAddresses` | 186 | Trimble tilladte IP-adresser (semikolon-separeret) |
| `LastStatisticsCalculationDate` | 156 | Dato for seneste statistikberegning |
| `LastFinnishCompanyRegistrationSyncDate` | 222 | Seneste finsk CVR-synk dato |
| `LastNorwegianAddressChangeIdSentToFramweb` | 230 | Sidst sendt norsk adresseændrings-ID til Framweb |
| `FramwebZipPassword` | 227 | Framweb ZIP-password |
| `FramwebApiKey` | 233 | Framweb API-nøgle |
| `WorkerApiUrl` | 280 | Worker API URL |
| `WorkerApiApplicationIdUri` | 281 | Worker API application ID URI |
| `HashSecret` | 290 | Hemmelig nøgle til hash-operationer |
| `EnableCustomerSurveyNudging` | 203 | Aktivér kundeundersøgelses-nudging |
| `HereMapsDriftstatusApiKey` | 204 | HERE Maps driftstatus API-nøgle |
| `HereMapsAppApiKey` | 205 | HERE Maps app API-nøgle |

---

## green-ai implementeringsregler

```
✅ Alle nøgler SKAL registreres i dette enum med korrekt numerisk ID
✅ Ny nøgle: tilføj enum-felt + kald CreateDefault() ved app-start
✅ Cache-nøgle: "Sms.applicationsettings" (bevares for kompatibilitet)
✅ Get() returnerer altid en instans — opret default ved første brug
❌ Aldrig hent settings med raw string-nøgler
❌ Aldrig gem sensitive værdier (API keys) i kode — altid via AppSetting
```

---

## Fase 1 — minimum nøgler til foundation (SLICE-001)

Disse nøgler er tilstrækkelige til at foundation-slices virker:

| Nøgle | Bruges af |
|-------|-----------|
| `RequestLogLevel` (160) | SLICE-005 `RequestResponseLoggingMiddleware` |
| `DisableAllBroadcasts` (184) | Alle messaging-features (read-only gate) |
| `DisableAllSystemMessages` (206) | System-notifikationer (read-only gate) |

Øvrige nøgler tilføjes progressivt når den pågældende feature-domæne implementeres.
