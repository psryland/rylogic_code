module prd.test;

import prd.all;

void main(string[] args)
{
	Loc loc = Loc("file", 0, 0);
	loc.adv('1');
	loc.adv('\n');
	loc.adv('\n');
	loc.adv('2');
	assert(loc.m_line == 2);
	assert(loc.m_col == 1);
	
	Loc loc2 = loc;
	assert(loc2 == loc);
	
	loc.adv('a');
	assert(loc2 != loc);
	assert(loc2 < loc);
	//prd.storage.settings.RunTest();
}

//import std.stdio;
//import std.file;
//import std.exception;
//import std.conv;
//import prd.all;
//
//void main(string[] args)
//{
//    try
//    {
//        std.stdio.write("pr.db.sqlite unittest ... ");
//        {
//            string dbfile = "testdb.db";
//            scope(exit) std.file.remove(dbfile);
//            {
//                scope SqliteDatabase db = new SqliteDatabase(dbfile);
//                db.execute("create table if not exists \"Table\" (key integer primary key, value text);");
//                db.execute("insert into \"Table\" (value) values ('The first string');");
//                db.execute("insert into \"Table\" (value) values ('The second string');");
//            }
//        }
//        std.stdio.writeln("complete");
//    }
//    catch (Throwable ex)
//    {
//        std.stdio.writeln("failed.\n" ~ ex.toString());
//    }
//}

//import dgui.all;

//class MainForm :Form
//{
//    private Button _btnOk;
//    
//    public this()
//    {
//        this.text = "DGui Events";
//        this.size = Size(300, 250);
//        this.startPosition = FormStartPosition.CENTER_SCREEN; // Set Form Position
//        
//        this._btnOk = new Button();
//        this._btnOk.text = "Click Me!";
//        this._btnOk.dock = DockStyle.FILL; // Fill the whole form area
//        this._btnOk.parent = this;
//        _btnOk.click.attach(&this.onBtnOkClick); //Attach the click event with the selected procedure
//    }
//    
//    private void onBtnOkClick(Control sender, EventArgs e)
//    {
//        // Display a message box
//        MsgBox.show("OnClick", "Button.onClick()");
//    }
//}
//
//int main(string[] args)
//{
//    return Application.run(new MainForm()); // Start the application
//} 


/*module main;

import std.stdio;
import std.math;
import std.datetime;
import std.c.stdio;
import dgui.
// import prd.maths.rand;
// import prd.maths.maths;

extern(C) int kbhit();
extern(C) int getch();

int main(string[] argv)
{
//	StopWatch sw;
//	sw.start();
//	for (int i = 0; i != 1000; ++i)
//	{
//		v4 a = v4.random4();
//		a /= FRand();
//	}
//	sw.stop();
//	writeln("time: ", sw.peek().microseconds, "us\n");
	writeln("Hello World\n");
	while (!kbhit()) {}
	return 0;
}
	
 */