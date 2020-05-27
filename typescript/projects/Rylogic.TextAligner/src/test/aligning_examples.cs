/* Aligning assignment expressions */
assi   = gnment
assign += ment
ass-= ignment
assignm *= ent
assi/= gnment
as %= signment
assignme ^= nt
assignm~= ent
assi &= gnment
ass |= ignment
assi ||= gnment
as &&= signment

/* Lambdas */
(a) => {}
(a,b,c) => { .. }
_ => one;

/* Comparisons */
equ == ality
inequ!= ality
inequal<= ity
in >= equality
ineq< uality
inequa > lity

/* Mixed assignments, comparisons, and lambdas */
assign += ment
assi/= gnment
lamb => da
as %= signment
assignme ^= nt
equ == ality
ass -= ignment
assignm ~= ent
ass |= ignment
lamb => da
inequ!= ality
in >= equality
inequa > lity
ineq< uality
lamb => da
inequal<= ity
assi ||= gnment
assignm *= ent
assi &= gnment
lamb => da
as &&= signment

/* Boolean operators */
Boolean && bool
Or || Oar

/* Line comments with only whitespace before them - should align to whichever line the caret is on */
	// work
		// stuff
	// aligner
	// worx
			// yay

/* Comments following text - should align with one whitespace character past the longest non-comment */
words	// work
more words	// stuff
code // aligner
characters   // worx
other stuff 	// yay

/* Brackets - Opens should align with opens, closes with closes. */
open( open( open ( close) open( close)
open( close) open ( close) open( close)

/* Scope braces - Opens with opens, closes with closes */
start { stop } stop } start { start{
stop} start { start { stop } stop }

/* Increment/Decrement */
a++ ++b --c
 --d  e-- ++f

/* Plus/minus - Create aligned tabular data */
+0.0 -10.0 +123.0 -0.4
  +0.2 +111.0 -321.0 +10

/* Commas */
Over, use, of, commas, in,
a, sentence, that, doesnt, need, commas,

/* Class members */
int m_int;
float m_float;
double _double;
char _char;
string str_;
long long_;
