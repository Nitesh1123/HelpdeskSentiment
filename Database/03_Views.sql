-- =============================================================
--  User defined views
--  These are also created automatically by the EF Core migration
--  "AddViewsAndProcedures", so the app and SSMS see the same objects.
-- =============================================================
USE HelpdeskSentimentDb;
GO

-- Simple view: one row per sentiment label with totals.
CREATE OR ALTER VIEW dbo.vw_SentimentSummary
AS
SELECT  Sentiment,
        COUNT(*)                      AS TotalCalls,
        CAST(AVG(SentimentScore) AS DECIMAL(5,3)) AS AverageScore,
        SUM(CASE WHEN IsEscalated = 1 THEN 1 ELSE 0 END) AS EscalatedCalls
FROM    dbo.CallRecords
GROUP BY Sentiment;
GO

-- Complex view (JOIN): how many negative calls each agent handled.
CREATE OR ALTER VIEW dbo.vw_AgentSentiment
AS
SELECT  a.Id   AS AgentId,
        a.Name AS AgentName,
        COUNT(c.Id) AS TotalCalls,
        SUM(CASE WHEN c.Sentiment = 'Negative' THEN 1 ELSE 0 END) AS NegativeCalls,
        SUM(CASE WHEN c.Sentiment = 'Positive' THEN 1 ELSE 0 END) AS PositiveCalls,
        CAST(ISNULL(AVG(c.SentimentScore), 0) AS DECIMAL(5,3))   AS AverageScore
FROM    dbo.Agents a
LEFT JOIN dbo.CallRecords c ON c.AgentId = a.Id
GROUP BY a.Id, a.Name;
GO

-- Usage
SELECT * FROM dbo.vw_SentimentSummary;
SELECT * FROM dbo.vw_AgentSentiment ORDER BY NegativeCalls DESC;
GO
