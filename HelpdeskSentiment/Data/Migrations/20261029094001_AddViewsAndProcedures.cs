using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpdeskSentiment.Data.Migrations
{
    /// <summary>
    /// Creates the SQL Server views and stored procedures from Database/03_Views.sql and
    /// Database/04_StoredProcedures.sql so they exist as soon as the app runs.
    /// </summary>
    public partial class AddViewsAndProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER VIEW dbo.vw_SentimentSummary
AS
SELECT  Sentiment,
        COUNT(*)                      AS TotalCalls,
        CAST(AVG(SentimentScore) AS DECIMAL(5,3)) AS AverageScore,
        SUM(CASE WHEN IsEscalated = 1 THEN 1 ELSE 0 END) AS EscalatedCalls
FROM    dbo.CallRecords
GROUP BY Sentiment;");

            migrationBuilder.Sql(@"
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
GROUP BY a.Id, a.Name;");

            migrationBuilder.Sql(@"
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
END");

            migrationBuilder.Sql(@"
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
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_DailySentimentReport;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetCallsBySentiment;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_AgentSentiment;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_SentimentSummary;");
        }
    }
}
