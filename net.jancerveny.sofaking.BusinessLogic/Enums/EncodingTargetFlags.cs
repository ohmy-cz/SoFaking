using System;

[Flags]
public enum EncodingTargetFlags
{
	None = 0,
	NeedsNewVideo = 1 << 1,
	NeedsNewAudio = 1 << 2,
	VideoIsAnimation = 1 << 3
}