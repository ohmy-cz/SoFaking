using System;

[Flags]
public enum VideoCompatibilityFlags
{
	Compatible = 0,
	IncompatibleCodec = 1 << 1,
	IncompatibleResolution = 1 << 2,
	IncompatibleSize = 1 << 3,
	IncompatibleBitrate = 1 << 4
}