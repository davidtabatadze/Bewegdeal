-- Run this first to diagnose the issue
SELECT Id, Email, Role, Status FROM dev_Users ORDER BY Id;
SELECT COUNT(*) AS existing_requests FROM dev_Requests;
