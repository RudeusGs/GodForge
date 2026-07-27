# Jobs

PostgreSQL is the source of truth. RabbitMQ transports messages only.

Lifecycle:

```text
queued -> running -> completed
       -> retrying -> running
       -> cancelled | failed | timeout | dead_lettered
```

Every message includes schema version, message ID, job ID, project ID, repository ID when relevant, correlation ID, attempt count and input hash. Workers use bounded retries and dead-letter invalid/exhausted messages. Duplicate input returns an existing job/result when safe.
