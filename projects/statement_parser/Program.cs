using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using pr.util;

namespace statement_parser
{
	class Program
	{
		public class Transaction
		{
			public string   m_type;
			public string   m_details0;
			public string   m_details1;
			public string   m_details2;
			public string   m_reference;
			public decimal  m_amount;
			public DateTime m_date;

			public Transaction(List<string> row)
			{
				m_type = row[0];
				m_details0 = row[1];
				m_details1 = row[2];
				m_details2 = row[3];
				m_reference = row[4];
				m_amount = decimal.Parse(row[5]);
				m_date = DateTime.ParseExact(row[6], "dd/MM/yyyy", CultureInfo.InvariantCulture);
			}
			public override string  ToString()
			{
				 return m_date.ToShortDateString()+","+m_amount+","+m_type+","+m_details0+","+m_details1+","+m_details2+","+m_reference;
			}
		}

		public class Account
		{
			public string m_acct_name;
			public decimal m_final_balance;
			public DateTime m_start_date;
			public List<Transaction> m_transactions = new List<Transaction>();

			public Account(string acct_filename, decimal final_balance)
			{
				m_acct_name = Path.GetFileNameWithoutExtension(acct_filename);
				m_final_balance = final_balance;
				m_start_date = DateTime.MaxValue;
				
				CSVData csv = CSVData.Load(acct_filename);
				foreach (var row in csv.Rows)
				{
					Transaction trans = new Transaction(row);
					m_transactions.Add(trans);
					if (trans.m_date < m_start_date) m_start_date = trans.m_date;
				}
			}
		}

		static void Main(string[] args)
		{
			try
			{
				string output_csv_filename = args[0];
				int days = 0;

				// Load each listed account
				List<Account> accts = new List<Account>();
				for (int i = 1; i < args.Length; i += 2)
				{
					string acct_filename = args[i];
					decimal final_balance = decimal.Parse(args[i+1]);
					Account acct = new Account(acct_filename, final_balance);
					accts.Add(acct);
					days = Math.Max(days, (DateTime.Today - acct.m_start_date).Days);
				}

				// Create the output csv
				CSVData csv = new CSVData{AutoSize = true};
				csv.Reserve(days, accts.Count + 1);

				// Account columns
				int col = 1;
				foreach (Account acct in accts)
				{
					csv[0,col] = acct.m_acct_name;
					
					int index = 0;
					DateTime day = DateTime.Today;
					decimal balance = acct.m_final_balance;
					for (int i = 0; i != days; ++i)
					{
						for (; index != acct.m_transactions.Count && acct.m_transactions[index].m_date >= day; ++index)
							balance -= acct.m_transactions[index].m_amount;

						csv[1+i,col] = balance.ToString("G");
						day = day.AddDays(-1);
					}

					++col;
				}

				// Date column
				{
					csv[0,0] = "Date";
					DateTime day = DateTime.Today;
					for (int i = 0; i != days; ++i, day = day.AddDays(-1))
						csv[1+i,0] = day.ToShortDateString();
				}

				csv.Save(output_csv_filename);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.WriteLine("Syntax: statement_parser output_csv_filename [acct_number.csv final_balance] [acct_number.csv final_balance] ...");
			}
		}
	}
}
