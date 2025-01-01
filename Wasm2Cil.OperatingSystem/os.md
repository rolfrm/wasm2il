# A User-Space WebAssembly Operating system (Experiment)

The goal: A secure modular operating system capable of running the same code on all platforms.

Applications describe a limited set of imports that they use. The OS or parent app provides the API implementations and drivers.

So for example lets say you have an app which needs to use a camera feed.

```c#
__attribute__((import_module("libcam")))
int libcam_listcameras(cam_desc * buffer, int count);

__attribute__((import_module("libcam")))
int libcam_open(int camid);
__attribute__((import_module("libcam")))
void libcam_read_frame(int camid, void * buffer);
__attribute__((import_module("libcam")))
int libcam_close(int camid);
```

The operating system can provide a libcam, but so can the parent process. This way, the parent process can monitor what the child process does by overriding the imports.

In some security related cases, the parent process should not be allowed overriding the call. This can be controlled on a per-module level. For example, lets say I want to do a payment from a user app, that should never be interceptable from the parent app. 