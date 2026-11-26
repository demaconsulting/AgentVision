## CanvasNet

This document describes the integration and usage design for the `CanvasNet` OTS software item (the
`DemaConsulting.CanvasNet` NuGet package, a .NET imaging library of DEMA Consulting). It is the library that the
AgentVisionCore system uses at run time. (The CanvasNet command-line tool that renders the diagrams of the
documentation is a separate, build-time use, covered by the documentation tooling and not by this item.)

### Purpose

CanvasNet decodes images, crops them, and writes them as files. It was chosen because it is a dependency-free .NET
library (no native assets, so the AgentVision packages stay free of them), because it reads and writes every format that
the product needs (JPEG, PNG, and BMP), and because it is developed by the same organization. It is a runtime dependency
of the AgentVisionCore library and flows to consumers as a package dependency.

### Features Used

- **The image decoder** that chooses a codec from the first bytes of the data and returns a pixel surface; it reads JPEG
  (baseline and progressive), PNG, and uncompressed 24-bit and 32-bit BMP (bottom-up), and reports data that is empty,
  unrecognized, or damaged with an invalid-data exception.
- **The pixel surface**: width, height, pixel access, and the crop operation that copies a region into a new surface
  with its own pixels and rejects a region that is outside the surface.
- **The PNG, JPEG, and BMP encoders**: write a surface to a stream; PNG with or without an alpha channel, JPEG with a
  quality, and BMP with 24 or 32 bits.

Not used: drawing, text, fonts, resampling, color adjustments, comparison, and the other image formats, which are used
by the documentation tooling and not by the library.

### Integration Pattern

Only the `FrameDecoder` and `CameraImage` units reference CanvasNet, and CanvasNet types never cross the public API of
the library: a test inspects every public signature for them. A decoded surface is owned by the `CameraImage` that
wraps it and is disposed with it. CanvasNet reads three kinds of frame defects differently from what cameras send, which
the `FrameDecoder` unit repairs before decoding: a JPEG without Huffman tables, a top-down bitmap, and a 32-bit bitmap
whose unused fourth byte is zero (read as transparency); see *FrameDecoder*. CanvasNet limits a picture to 8192 pixels
on each side, far above any webcam. The package is a pre-release; the library therefore suppresses the NuGet warning
that a stable package must not depend on a pre-release package (with the reason documented in the project file), and
the suppression is removed when CanvasNet has a stable release. Upgrading the package requires re-running the
library tests, which are the evidence for the CanvasNet requirements, and a review of this design.
