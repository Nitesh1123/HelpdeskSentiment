-- =============================================================
--  DML commands, clauses, operators and joins demo
--  Run in SSMS against (localdb)\MSSQLLocalDB after the app has
--  created the database.
-- =============================================================
USE HelpdeskSentimentDb;
GO

-- INSERT
INSERT INTO dbo.Agents (Name, Email) VALUES (N'Demo Agent', N'demo@helpdesk.local');

INSERT INTO dbo.CallRecords (CallerName, PhoneNumber, Category, AgentId, Transcript, CallTime, DurationSeconds, SentimentScore, Sentiment, IsEscalated, Keywords)
VALUES (N'SQL Demo Caller', N'9000000001', N'Billing',
        (SELECT Id FROM dbo.Agents WHERE Email = N'demo@helpdesk.local'),
        N'Inserted from SQL to demo DML.', SYSDATETIME(), 90, 0, N'Neutral', 0, NULL);

-- SELECT with WHERE, ORDER BY, comparison and logical operators
SELECT Id, CallerName, Category, Sentiment, SentimentScore
FROM dbo.CallRecords
WHERE Sentiment = N'Negative' AND DurationSeconds > 300
ORDER BY SentimentScore ASC;

-- LIKE, IN, BETWEEN operators
SELECT CallerName, Transcript FROM dbo.CallRecords WHERE Transcript LIKE N'%refund%';
SELECT CallerName, Category  FROM dbo.CallRecords WHERE Category IN (N'Billing', N'Complaint');
SELECT CallerName, SentimentScore FROM dbo.CallRecords WHERE SentimentScore BETWEEN -0.15 AND 0.15;

-- GROUP BY with aggregate functions and HAVING
SELECT Category, COUNT(*) AS Calls, AVG(SentimentScore) AS AvgScore
FROM dbo.CallRecords
GROUP BY Category
HAVING COUNT(*) >= 1
ORDER BY AvgScore;

-- INNER JOIN and LEFT JOIN
SELECT c.CallerName, a.Name AS AgentName, c.Sentiment
FROM dbo.CallRecords c
INNER JOIN dbo.Agents a ON a.Id = c.AgentId;

SELECT a.Name, COUNT(c.Id) AS TotalCalls
FROM dbo.Agents a
LEFT JOIN dbo.CallRecords c ON c.AgentId = a.Id
GROUP BY a.Name;

-- UPDATE
UPDATE dbo.CallRecords
SET IsEscalated = 1
WHERE Sentiment = N'Negative' AND SentimentScore <= -0.6;

-- DELETE (removes only the demo rows created above)
DELETE FROM dbo.CallRecords WHERE CallerName = N'SQL Demo Caller';
DELETE FROM dbo.Agents WHERE Email = N'demo@helpdesk.local';
GO
