-- Generates the tables and indices used by EDTradeAdvisor

pragma foreign_keys = ON;
pragma synchronous = OFF;
pragma temp_store = memory;
pragma journal_mode = memory;
pragma case_sensitive_like = false;

-- Systems table
create table if not exists [StarSystems] (
	[ID] integer unique primary key,
	[Name] text collate nocase,
	[X] real not null,
	[Y] real not null,
	[Z] real not null,
	[Population] integer not null,
	[NeedPermit] integer not null,
	[UpdatedAt] integer not null
);
create index if not exists IX_SystemByName on StarSystems([Name]);

-- Stations table
create table if not exists [Stations] (
	[ID] integer unique primary key,
	[Name] text collate nocase,
	[SystemID] integer not null,
	[Type] integer not null,
	[Distance] integer null,
	[Facilities] integer not null,
	[MaxPadSize] integer not null,
	[Planetary] integer not null,
	[UpdatedAt] integer not null,

	foreign key ([SystemID]) references StarSystems([ID])
		on update cascade
		on delete cascade
);
create index if not exists IX_StationBySystem on Stations([SystemID], [ID]);
create index if not exists IX_StationByName on Stations([Name]);

-- Commodity category table
create table if not exists [CommodityCategories] (
	[ID] integer unique primary key,
	[Name] text collage nocase
);

-- Commodities table
create table if not exists [Commodities] (
	[ID] integer unique primary key,
	[Name] text collate nocase,
	[CategoryID] integer not null,
	[IsRare] integer not null,
	[MinBuyPrice] integer null,
	[MaxBuyPrice] integer null,
	[MinSellPrice] integer null,
	[MaxSellPrice] integer null,
	[AveragePrice] integer null,
	[BuyPriceLowerAverage] integer not null,
	[SellPriceUpperAverage] integer not null,
	[IsNonMarketable] integer not null,
	[EDID] integer not null,

	foreign key ([CategoryID]) references CommodityCategories([ID])
		on update cascade
		on delete cascade
);

-- Listings Table
create table if not exists [Listings] (
	[ID] integer unique primary key,
	[StationID] integer not null,
	[CommodityID] integer not null,
	[Supply] integer not null,
	[SupplyBracket] integer not null,
	[BuyPrice] integer not null,
	[SellPrice] integer not null,
	[Demand] integer not null,
	[DemandBracket] integer not null,
	[UpdatedAt] integer not null,

	foreign key ([StationID]) references Stations([ID])
		on update cascade
		on delete cascade
	foreign key ([CommodityID]) references Commodities([ID])
		on update cascade
		on delete cascade
);
