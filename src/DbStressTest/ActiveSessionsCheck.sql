--return running, sleeping and other status sessions in current DB
SELECT N'running' as [status], COUNT(1) [#sessions] FROM SYS.DM_EXEC_SESSIONS 
WHERE [status] = N'running'
UNION ALL
SELECT N'sleeping' as [status], COUNT(1) FROM SYS.DM_EXEC_SESSIONS 
WHERE [status] = N'sleeping'
UNION ALL
SELECT N'others' as [status], COUNT(1) FROM SYS.DM_EXEC_SESSIONS 
WHERE [status] NOT IN (N'sleeping', N'running')
