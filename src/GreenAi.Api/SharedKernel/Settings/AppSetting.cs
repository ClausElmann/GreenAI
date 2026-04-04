namespace GreenAi.Api.SharedKernel.Settings;

/// <summary>
/// Strongly typed enum for alle systemkonfigurationsnøgler.
/// Numeriske IDs er stabile og svarer til ApplicationSettings.ApplicationSettingTypeId i DB.
/// Tilføj nyt felt + kald CreateDefault() i IApplicationSettingService ved ny nøgle.
/// Se docs/SSOT/backend/reference/appsetting-enum.md for kategoriseret reference.
/// </summary>
public enum AppSetting
{
    // Gateway / SMS
    GatewayAPISingleToken       = 33,
    GatewayAPISingleTokenSE     = 176,
    GatewayAPISingleTokenFI     = 177,
    GatewayAPISingleTokenNO     = 187,
    GatewayAPISingleTokenHighPrio = 185,
    GatewayAPIUrl               = 183,

    // Kill-switches
    DisableAllBroadcasts        = 184,
    DisableAllSystemMessages    = 206,
    DisableMessageBackgroundService = 202,

    // Email (SendGrid)
    SendGridAPIKey              = 130,
    SendGridHost                = 229,

    // Voice (Infobip)
    VoiceGateway                = 142,
    InfobipApiKey               = 143,
    InfobipApiEndpoint          = 144,

    // Azure Batch
    BatchAppProgramVersion      = 100,
    BatchAccountName            = 101,
    BatchAccountKey             = 102,
    BatchAccountUrl             = 103,
    BatchPoolName               = 104,
    BatchAppCommandLine         = 105,

    // Logging
    RequestLogLevel             = 160,

    // Adresseopslag — Norge
    NorwegianPhoneLookupApiKey  = 188,
    NorwegianPhoneExtendedApiKey = 189,
    NorwegianApi1881Url         = 190,
    NorwegianPublicContactsAccessKey  = 207,
    NorwegianPublicContactsAccessToken = 208,
    NorwegianPublicContactsApiUrl = 209,
    NorwegianSoapEndpointAddress = 223,
    NorwegianSoapUserName       = 224,
    NorwegianSoapPassword       = 225,
    LastNorwegianAddressChangeId = 228,
    LastNorwegianAddressChangeIdSentToFramweb = 230,

    // Adresseopslag — Sverige
    LantmäterietKey             = 220,
    LantmäterietSecret          = 221,
    LantmäterietUserName        = 260,
    LantmäterietUserPassword    = 261,
    LantmäterietMapUserName     = 239,
    LantmäterietMapPassword     = 240,

    // Adresseopslag — Danmark
    OwnerFilePattern            = 152,
    OwnerDatafordelerCertFileName = 153,
    OwnerDatafordelerHost       = 154,
    OwnerDatafordelerUserName   = 155,
    AddressBfeExclusion         = 226,
    EjerfortegnelseCertificateThumbprint = 169,

    // Robinson (DK)
    RobinsonFTPhost             = 90,
    RobinsonFTPuser             = 91,
    RobinsonSFTPpassword        = 180,
    RobinsonFTPlatestFilename   = 94,

    // eBoks
    EboksCvrApiCertificateThumbprint = 167,

    // Statstidende
    StatstidendeCertificateThumbprint = 170,

    // Vejrvarsler
    WeatherWarningHost          = 149,
    WeatherWarningFilename      = 150,
    WeatherWarningMD5Filename   = 151,
    WeatherWarningApiKey        = 241,

    // Strex
    StrexApiUrl                 = 270,
    EnableStrex                 = 271,
    StrexApiKeyDK               = 275,
    StrexApiKeyFI               = 276,
    StrexApiKeyNO               = 277,
    StrexApiKeySE               = 278,
    EnableStrexDK               = 300,
    EnableStrexSE               = 301,
    EnableStrexFI               = 302,
    EnableStrexNO               = 303,

    // Maskinporten / KoFuVi (Norge)
    MaskinportenLoginCertificateThumbprint = 234,
    KoFuViCertificateThumbprint = 235,
    MaskinportenApiUrl          = 236,
    KoFuViApiUrl                = 237,
    KrrApiUrl                   = 238,

    // Sociale medier
    FacebookAppId               = 145,
    FacebookAppSecret           = 146,
    TwitterConsumerKey          = 147,
    TwitterConsumerSecret       = 148,

    // Tony / Fact24
    TonyStatusStreamUrl         = 242,
    TonyCallsAPIUrl             = 243,
    Fact24AuthUrl               = 244,
    Fact24AuthClientId          = 245,
    Fact24AuthClientSecret      = 246,

    // Diverse
    TeleAdressUserID            = 120,
    TeleAdressPassword          = 121,
    DropboxClientKey            = 168,
    ProfinderApiKey             = 171,
    QuickResponseRandomChars    = 210,
    TrimbleIpAddresses          = 186,
    LastStatisticsCalculationDate = 156,
    LastFinnishCompanyRegistrationSyncDate = 222,
    FramwebZipPassword          = 227,
    FramwebApiKey               = 233,
    WorkerApiUrl                = 280,
    WorkerApiApplicationIdUri   = 281,
    HashSecret                  = 290,
    EnableCustomerSurveyNudging = 203,
    HereMapsDriftstatusApiKey   = 204,
    HereMapsAppApiKey           = 205,
}
