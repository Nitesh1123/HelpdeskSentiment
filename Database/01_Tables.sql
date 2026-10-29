-- =============================================================
--  HelpdeskSentimentDb : tables and constraints
--  The app creates these with EF Core Code First migrations.
--  This script shows the same schema in plain T-SQL (DDL) so it
--  can be run by hand in SSMS for the Database First approach.
-- =============================================================

IF DB_ID('HelpdeskSentimentDb') IS NULL
    CREATE DATABASE HelpdeskSentimentDb;
GO
USE HelpdeskSentimentDb;
GO

IF OBJECT_ID('dbo.Agents', 'U') IS NULL
CREATE TABLE dbo.Agents
(
    Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Agents PRIMARY KEY,
    Name    NVARCHAR(100) NOT NULL,
    Email   NVARCHAR(150) NOT NULL CONSTRAINT UQ_Agents_Email UNIQUE
);
GO

IF OBJECT_ID('dbo.CallRecords', 'U') IS NULL
CREATE TABLE dbo.CallRecords
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CallRecords PRIMARY KEY,
    CallerName      NVARCHAR(100)  NOT NULL,
    PhoneNumber     NVARCHAR(10)   NOT NULL CONSTRAINT CK_CallRecords_Phone CHECK (LEN(PhoneNumber) = 10),
    Category        NVARCHAR(20)   NOT NULL CONSTRAINT DF_CallRecords_Category DEFAULT ('General'),
    AgentId         INT            NULL
                    CONSTRAINT FK_CallRecords_Agents_AgentId REFERENCES dbo.Agents(Id) ON DELETE SET NULL,
    Transcript      NVARCHAR(4000) NOT NULL,
    CallTime        DATETIME2      NOT NULL CONSTRAINT DF_CallRecords_CallTime DEFAULT (SYSDATETIME()),
    DurationSeconds INT            NOT NULL CONSTRAINT CK_CallRecords_Duration CHECK (DurationSeconds BETWEEN 1 AND 7200),
    SentimentScore  FLOAT          NOT NULL CONSTRAINT CK_CallRecords_Score CHECK (SentimentScore BETWEEN -1 AND 1),
    Sentiment       NVARCHAR(10)   NOT NULL CONSTRAINT CK_CallRecords_Sentiment CHECK (Sentiment IN ('Positive', 'Neutral', 'Negative')),
    IsEscalated     BIT            NOT NULL CONSTRAINT DF_CallRecords_IsEscalated DEFAULT (0),
    Keywords        NVARCHAR(500)  NULL
);
GO

CREATE INDEX IX_CallRecords_Sentiment ON dbo.CallRecords(Sentiment);
CREATE INDEX IX_CallRecords_AgentId   ON dbo.CallRecords(AgentId);
GO
