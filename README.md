![logo](/resources/logo/logo_100x100.png)
# DatabaseStressTestTool
Database Stress Test Tool to test database connection etc limits

## ABOUT

This is a database stress tool. Project page: https://github.com/Oriflame/DatabaseStressTestTool

Copyright: [Oriflame Software](http://corporate.oriflame.com/About_Oriflame/), follow us on [Facebook](https://www.facebook.com/oriflamesoftware) or [LinkedIn](https://www.linkedin.com/company/165341).

Please note: this software is provided "as is", without support. If you fidn a bug, want to contribute, ... I would be more than happy, if you contact me via my [GitHub profile](https://github.com/jvilimek) or [LinkedIn](https://cz.linkedin.com/in/jvilimek), but I can not guarantee I will be able to answer.

## EXAMPLES of usage

some basic stress on local DB:

```
DbStressTest.exe -c "Data Source=.\SQLEXPRESS;Trusted_Connection=True;ConnectRetryCount=5;ConnectRetryInterval=2;Timeout=30;Enlist=false;Max Pool Size=25;" -t 40 -d 120 -s -e GetNewId -r 600
DbStressTest.exe -c "Data Source=.\SQLEXPRESS;Trusted_Connection=True;ConnectRetryCount=5;ConnectRetryInterval=2;Timeout=30;Enlist=false;Max Pool Size=100;" -t 200 -d 120 -s -e GetNewId -r 100
```

tests on Azure DB (actual user / password for the purpose of Microsoft tests; usage for unauthorized users not allowed):
```
DbStressTest.exe -c "user id=...;password=...;Data Source=....database.windows.net;Database=db;ConnectRetryCount=5;ConnectRetryInterval=2;Timeout=30;Enlist=false;Max Pool Size=100;" -t 200 -d 120 -s -e GetNewId -r 100
DbStressTest.exe -c "user id=...;password=...;Data Source=....database.windows.net;Database=db;ConnectRetryCount=5;ConnectRetryInterval=2;Timeout=30;Enlist=false;Max Pool Size=100;Min pool size=100;" -t 200 -d 120 -s -e GetNewId -r 100
```
## Why there is a need of the DB Stress test?

We wanted to isolate issues we have with SQL Azure databases under high load. 
Apart of few transient errors, we got errors like:

```
A transport-level error has occurred when receiving results from the server. (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
A transport-level error has occurred when receiving results from the server. (provider: TCP Provider, error: 0 - The semaphore timeout period has expired.)
```

## History

```
1.5 ... sample time for DbMonitoring now in settings
        fix for console.writeline thread lock issue
        new SqlTransientError check
        fix in stats
1.4 ... Added database monitoring (-m switch), new BatchType: RandomMix and NoSqlAction
```
Previous changes are undocumented.
