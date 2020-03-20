using System;

[Flags]
public enum GenreFlags
{
	Other = 0,
	Action = 1 << 1, 
	Adventure = 1 << 2,
	Animation = 1 << 3,
	Biography = 1 << 4,
	Comedy = 1 << 5,
	Crime = 1 << 6,
	Drama = 1 << 7,
	Fantasy = 1 << 8,
	Family = 1 << 9,
	Historical = 1 << 10,
	Horror = 1 << 11,
	Mystery = 1 << 12,
	Romance = 1 << 13,
	SciFi = 1 << 14,
	Thriller = 1 << 15,
	War = 1 << 16,
	Western = 1 << 17
}
