-- =============================================================
--  User defined stored procedures
--  Also created by the EF Core migration "AddViewsAndProcedures".
-- =============================================================
USE HelpdeskSentimentDb;
GO

-- Calls filtered by sentiment label (pass NULL for all).
CREATE OR ALTER PROCEDURE dbo.sp_GetCallsBySentiment
    @Sentiment NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.Id, c.CallerName, c.PhoneNumber, c.Category, a.Name AS AgentName,
           c.CallTime, c.SentimentScore, c.Sentiment, c.IsEscalated
    FROM   dbo.CallRecords c
    LEFT JOIN dbo.Agents a ON a.Id = c.AgentId
    WHERE  @Sentiment IS NULL OR c.Sentiment = @Sentiment
    ORDER BY c.CallTime DESC;
END
GO

-- Day-wise report of call counts and average score for the last @Days days.
CREATE OR ALTER PROCEDURE dbo.sp_DailySentimentReport
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CallTime AS DATE) AS CallDate,
           COUNT(*) AS TotalCalls,
           SUM(CASE WHEN Sentiment = 'Positive' THEN 1 ELSE 0 END) AS Positive,
           SUM(CASE WHEN Sentiment = 'Neutral'  THEN 1 ELSE 0 END) AS Neutral,
           SUM(CASE WHEN Sentiment = 'Negative' THEN 1 ELSE 0 END) AS Negative,
           CAST(AVG(SentimentScore) AS DECIMAL(5,3)) AS AverageScore
    FROM   dbo.CallRecords
    WHERE  CallTime >= DATEADD(DAY, -@Days, CAST(SYSDATETIME() AS DATE))
    GROUP BY CAST(CallTime AS DATE)
    ORDER BY CallDate;
END
GO

-- Usage
EXEC dbo.sp_GetCallsBySentiment @Sentiment = 'Negative';
EXEC dbo.sp_DailySentimentReport @Days = 30;
GO
