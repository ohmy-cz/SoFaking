using System;

[Flags]
public enum EncodingTargetFlags
{
	None = 0,
	Video = 1 << 1,
	Audio = 1 << 2
}