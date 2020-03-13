using System;

[Flags]
public enum GenreFlags
{
	Other = 0,
	Action = 1 << 1, 
	Adventure = 1 << 2,
	Animated = 1 << 3,
	Comedy = 1 << 4,
	Crime = 1 << 5,
	Drama = 1 << 6,
	Fantasy = 1 << 7,
	Historical = 1 << 8,
	Horror = 1 << 9,
	Kids = 1 << 10,
	Mystery = 1 << 11,
	Romantic = 1 << 12,
	ScienceFiction = 1 << 13,
	Thriller = 1 << 14,
	Western = 1 << 15
}
