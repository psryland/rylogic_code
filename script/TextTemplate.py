# Expands a template file
import sys, re

# Template expander
# 'template_filepath' is the template file
# 'output_filepath' is the output file to create
# 'regex_pattern' is a regular expression pattern for the template fields
# 'subst_func' is the callback function that does the text substitution
# To support include files, use a fancy regex pattern and return the entire
# contents of the included file as the result of 'subst_func'
def Expand(template_filepath, output_filepath, regex_pattern, subst_func):
    pat = re.compile(regex_pattern)
    with open(template_filepath) as f:
        buf = f.read(-1)
        s = 0
        while True:
            m = pat.search(buf, s)
            if not m: break
            subst = subst_func(m)
            buf = buf[:m.start()] + subst + buf[m.end():]
            s = m.start()
    with open(output_filepath, mode='w') as f:
        f.write(buf)    

#Usage:
#def Subst(match):
#    print(match.group()[1:-1])
#    return match.group()[1:-1]
#Expand(r"template.txt", r"template_output.txt", r"\[([-_\w]+)\]", Subst)
