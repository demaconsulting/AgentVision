### CameraSelector

![Capture Structure](CaptureView.svg)

#### Purpose

`CameraSelector` decides which camera and which format a request means. It exists as a separate unit because these
two decisions are the policy that is easiest to get wrong and hardest to test through hardware: how a user's words
map to exactly one camera, and which of the formats a camera offers is the best match for a request. Both are pure
functions of their arguments, so every rule is tested exactly.

#### Data Model

The unit is an internal static class without state. Its constants are the policy values: the preferred order of the
pixel formats (`JPEG`, `YUYV`, `NV12`, `UYVY`, `RGB24`, `RGB32`, `ARGB32`, `PNG`; the formats `RGB8`, `RGB15`, and
`RGB16` and unknown formats are not candidates), the default size limit (1920x1080), and the default minimum frame
rate (15 frames per second).

#### Key Methods

**SelectCamera**: Finds the camera that a selector names.

- *Parameters*: the cameras in enumeration order, and the selector text.
- *Preconditions*: neither is null; the selector is not empty or white space.
- *Returns*: the camera.
- *Algorithm*: trim the selector; a non-negative integer is an index into the list (out of range is an error and is
  not retried as a name); otherwise an exact, ordinal identifier match wins; otherwise the cameras whose name equals
  the text ignoring letter case are collected, the backend of the first of them is taken (the list is ordered by
  backend, so this is the preferred backend), and more than one match inside that backend is an error that lists the
  index and identifier of each candidate. No match throws `CameraNotFoundException`.

**SelectFormat**: Chooses the format to open.

- *Parameters*: the camera, and the options (only the size and frame rate are used).
- *Returns*: one of the formats of the camera.
- *Algorithm*: keep the formats whose pixel format is in the preference list; if none remain throw
  `CameraOpenException`. Compute for each remaining format a key of numbers (smaller is better) and take the first
  format with the smallest key, comparing the numbers one after the other:
  - *No size requested*: rate gate (a requested rate that the format reaches, or without a request at least 15 frames
    per second or an unknown rate), rate distance, size within 1920x1080, area (larger is better inside the limit,
    smaller is better outside it), frame rate (higher is better), pixel-format preference.
  - *Size requested*: the sum of the absolute differences of the requested dimensions (an unspecified dimension counts
    zero), then the rate gate and distance when a rate was requested, then the frame rate, then the pixel-format
    preference.

#### Error Handling

A null argument gives `ArgumentNullException` and an empty selector gives `ArgumentException`. A selector without a
match gives `CameraNotFoundException`, whose message lists every available camera as index, name, and backend (or says
that none is available) and which carries the selector and the list. An ambiguous name gives `ArgumentException` that
names the index and identifier of each candidate. A camera without a usable format gives `CameraOpenException` that
names the camera and the supported pixel formats.

#### Dependencies

- **CameraInfo** and **CameraFormat** of the Cameras subsystem, and **CameraOpenOptions** of this subsystem (as input).
- **CameraNotFoundException** and **CameraOpenException** (as output).

#### Callers

`CameraOpener` and `CameraSnapper`.
