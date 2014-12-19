#pragma once

#include "SpaceTrucker/src/forward.h"

namespace st
{
	struct System
	{
		int    m_id;
		char   m_name[50];
		double m_loc_x;
		double m_loc_y;
		double m_loc_z;
		
		PR_SQLITE_TABLE(System ,"")
		PR_SQLITE_COLUMN(Id    ,m_id    ,integer ,"primary key autoincrement not null")
		PR_SQLITE_COLUMN(Name  ,m_name  ,text    ,"")
		PR_SQLITE_COLUMN(LocX  ,m_loc_x ,real    ,"")
		PR_SQLITE_COLUMN(LocY  ,m_loc_y ,real    ,"")
		PR_SQLITE_COLUMN(LocZ  ,m_loc_z ,real    ,"")
		PR_SQLITE_TABLE_END()

		System()
			:m_id()
			,m_name()
			,m_loc_x()
			,m_loc_y()
			,m_loc_z()
		{}
		System(int id, char const* name, double loc_x, double loc_y, double loc_z)
			:m_id(id)
			,m_name()
			,m_loc_x(loc_x)
			,m_loc_y(loc_y)
			,m_loc_z(loc_z)
		{
			strncpy(m_name, name, sizeof(m_name)-1);
		}
	};
	struct Station
	{
		int    m_id;
		int    m_system_id;
		int    m_body_id;
		char   m_name[50];
		double m_dist_from_star; // in Ls

		PR_SQLITE_TABLE(Station  ,"")
		PR_SQLITE_COLUMN(Id        ,m_id             ,integer ,"primary key autoincrement not null")
		PR_SQLITE_COLUMN(SystemId  ,m_system_id      ,integer ,"")
		PR_SQLITE_COLUMN(BodyId    ,m_body_id        ,integer ,"")
		PR_SQLITE_COLUMN(Name      ,m_name           ,text    ,"")
		PR_SQLITE_COLUMN(Dist      ,m_dist_from_star ,real    ,"")
		PR_SQLITE_TABLE_END()

		Station()
			:m_id()
			,m_system_id()
			,m_body_id()
			,m_name()
			,m_dist_from_star()
		{}
		Station(int id, int system_id, int body_id, char const* name, double dist_from_star)
			:m_id(id)
			,m_system_id(system_id)
			,m_body_id(body_id)
			,m_name()
			,m_dist_from_star(dist_from_star)
		{
			strncpy(m_name, name, sizeof(m_name)-1);
		}
	};
	struct Commodity
	{
		int    m_id;
		int    m_system_id;
		int    m_station_id;
		char   m_name[50];
		int    m_sell;
		int    m_buy;
		int    m_demand;
		int    m_supply;
		int    m_demand_level;
		int    m_supply_level;
		time_t m_last_update;

		PR_SQLITE_TABLE(Commodity   ,"")
		PR_SQLITE_COLUMN(Id         ,m_id           ,integer ,"primary key autoincrement not null")
		PR_SQLITE_COLUMN(SystemId   ,m_system_id    ,integer ,"")
		PR_SQLITE_COLUMN(StationId  ,m_station_id   ,integer ,"")
		PR_SQLITE_COLUMN(Name       ,m_name         ,text    ,"")
		PR_SQLITE_COLUMN(Sell       ,m_sell         ,integer ,"")
		PR_SQLITE_COLUMN(Buy        ,m_buy          ,integer ,"")
		PR_SQLITE_COLUMN(Demand     ,m_demand       ,integer ,"")
		PR_SQLITE_COLUMN(Supply     ,m_supply       ,integer ,"")
		PR_SQLITE_COLUMN(DemandLvl  ,m_demand_level ,integer ,"")
		PR_SQLITE_COLUMN(SupplyLvl  ,m_supply_level ,integer ,"")
		PR_SQLITE_COLUMN(LastUpdate ,m_last_update ,integer ,"")
		PR_SQLITE_TABLE_END()

		Commodity()
			:m_id          ()
			,m_system_id   ()
			,m_station_id  ()
			,m_name        ()
			,m_sell        ()
			,m_buy         ()
			,m_demand      ()
			,m_supply      ()
			,m_demand_level()
			,m_supply_level()
			,m_last_update ()
		{}
		Commodity(int id, int system_id, int station_id, char const* name, int sell, int buy, int demand, int supply, int demand_level, int supply_level, time_t last_update)
			:m_id          (id          )
			,m_system_id   (system_id   )
			,m_station_id  (station_id  )
			,m_name        (            )
			,m_sell        (sell        )
			,m_buy         (buy         )
			,m_demand      (demand      )
			,m_supply      (supply      )
			,m_demand_level(demand_level)
			,m_supply_level(supply_level)
			,m_last_update (last_update )
		{
			strncpy(m_name, name, sizeof(m_name)-1);
		}
	};

	struct TradeDB :pr::sqlite::Database
	{
		TradeDB(char const* db_filepath)
			:pr::sqlite::Database(db_filepath)
		{
			CreateTable<System>();
			CreateTable<Station>();
			CreateTable<Commodity>();

			// Register functions that sqlite can use
			// Here's how to create a function that finds the first character of a string.
			//static void firstchar(sqlite3_context *context, int argc, sqlite3_value **argv)
			//{
			//	if (argc == 1) {
			//		char *text = sqlite3_value_text(argv[0]);
			//		if (text && text[0]) {
			//			char result[2]; 
			//			result[0] = text[0]; result[1] = '\0';
			//			sqlite3_result_text(context, result, -1, SQLITE_TRANSIENT);
			//			return;
			//		}
			//	}
			//	sqlite3_result_null(context);
			//}

			//Then attach the function to the database.
			//sqlite3_create_function(db, "firstchar", 1, SQLITE_UTF8, NULL, &firstchar, NULL, NULL)

			//Finally, use the function in a sql statement.
			//SELECT firstchar(textfield) from table
		}

		void UseDummyData()
		{
			DropTable<System>();
			DropTable<Station>();
			DropTable<Commodity>();
			CreateTable<System>();
			CreateTable<Station>();
			CreateTable<Commodity>();

			auto sys_id = Table<System>().Insert(System(0, "LP 347-5", 49935.46875, 40957.3125, 24090.03125));
			auto stn_id = Table<Station>().Insert(Station(0, sys_id, 1, "Bernard City", 16.0));
			Table<Commodity>().Insert(Commodity(0,sys_id,stn_id,"Marine Equipment",3737,3681,1000,500,2,2, time(0)));
			Table<Commodity>().Insert(Commodity(0,sys_id,stn_id,"Beryllium"       ,7810,7696,1000,500,2,2, time(0)));
			Table<Commodity>().Insert(Commodity(0,sys_id,stn_id,"Indium"          ,5526,5445,1000,500,2,2, time(0)));
		}
	};
}