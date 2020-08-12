--
-- Generates the tables for 'history.db'
--

pragma foreign_keys = ON;
pragma synchronous = OFF;
pragma journal_mode = memory;
pragma case_sensitive_like = false;

-- Solar output table
create table if not exists [SolarOutput] (
	[ID] integer unique primary key,
	[Output] real not null,
	[Timestamp] integer not null
);

-- Consumer table
create table if not exists [Consumer] (
	[ID] integer unique primary key,
	[DeviceID] integer not null,
	[Timestamp] integer not null
)

