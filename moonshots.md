# Moonshot Ideas

## Remote Light Cache

The idea here is the following


1. Local requests a job to be executed
1. The remote job begins. Simultaneously, the remote light cache process looks
   up in a table the anticipated Windows APIs that executed for the process
   requested, and starts asking for the files and API call results before the
   remote job gets to them.
1. The local execution serves the remote light cache process requests.
1. If the API call matches a previous execution's results, the remote cache
   process continues to request API call results ahead of time.
1. When the API calls no longer match a previous execution of the job, the
   remote light cache process gives up and allows the remote process to execute
   the full job.
1. If the entire list of API calls from a previous remote job matches the
   results the local job is returning, the remote job is terminated and the
   remote light cache job returns the existing result from the cache.

This has a few useful properties:

* If the remote process takes a long time to get to the point where the next
  API call is requested, pre-requesting the API call result can significantly
  reduce the amount of time waiting for I/O.
* Clients would not need to compute their entire dependencies ahead of time,
  while still being able to cache the results.

### Remote Light Cache Moonshot: Process Checkpoints

Let's say we have the following two abilities:

* When EasyHook intercepts an API call, we can store the state of the thread's
  stack, heap, and program counter.
* When EasyHook executes a process, it is able to also spin up threads with
  the stack, heap, and program counter in a given way.

If we had both of these abilities, we could then pickle the process's execution
at any API call (of a process that only contains a single thread, e.g., most
compilers), allowing us to reload that state where the API call's result no
longer matches the previous execution. This could allow us to skip large
portions of the process's execution when a single file changes.

Is the time spent taking a snapshot of the process worth it? Probably not.
Is it cool? Yes.
