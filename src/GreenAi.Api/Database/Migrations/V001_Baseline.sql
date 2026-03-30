-- V001 Baseline
-- Customer (tenant root)
CREATE TABLE Customers
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(200)   NOT NULL,
    CreatedAt   DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

-- Users
CREATE TABLE Users
(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId      INT             NOT NULL REFERENCES Customers(Id),
    Email           NVARCHAR(256)   NOT NULL,
    PasswordHash    NVARCHAR(512)   NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION      NOT NULL
);

CREATE UNIQUE INDEX UIX_Users_Email ON Users(Email);

-- Profiles (a user can have multiple profiles within a customer)
CREATE TABLE Profiles
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId  INT             NOT NULL REFERENCES Customers(Id),
    UserId      INT             NOT NULL REFERENCES Users(Id),
    DisplayName NVARCHAR(200)   NOT NULL,
    CreatedAt   DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

-- Refresh tokens (single-use rotation)
CREATE TABLE UserRefreshTokens
(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId      INT             NOT NULL REFERENCES Customers(Id),
    UserId          INT             NOT NULL REFERENCES Users(Id),
    Token           NVARCHAR(512)   NOT NULL,
    ExpiresAt       DATETIMEOFFSET  NOT NULL,
    UsedAt          DATETIMEOFFSET  NULL,
    CreatedAt       DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE UNIQUE INDEX UIX_UserRefreshTokens_Token ON UserRefreshTokens(Token);
