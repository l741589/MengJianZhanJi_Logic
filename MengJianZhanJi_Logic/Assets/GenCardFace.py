#-*- coding: utf-8 -*-
from pypinyin import pinyin, lazy_pinyin
import pypinyin
import codecs

	
def toTable(cells):
	fields=cells[0];
	fieldRange=range(0,len(fields));
	table=[];
	for line in cells[1:]:
		obj={}
		for i in fieldRange:
			obj[fields[i]]=line[i];
		table.append(obj);
	return table;

def toCsv(table):
	cols=set([]);
	for rec in table:
		cols|=set(rec.keys());
	cols=list(cols);
	
	cells=[cols];
	for rec in table:
		#print rec;
		line=[None]*len(cols);
		for k,v in rec.items():
			line[cols.index(k)]=v;
		cells.append(line)
		
	return cells;
	
def bigCamel(s):
	if ord(s[0][0])>128: return "";
	s=str(s[0]);
	return s[0:1].upper()+s[1:];

def genSym(s):
	py=pinyin(unicode(s,"UTF-8"),style=pypinyin.NORMAL);
	return "".join([bigCamel(i) for i in py]);
	
	
f=open("CardInfo_raw.csv");
lines=f.readlines();
f.close();
cells=[[s.strip() for s in i.split("\t")] for i in lines];

table=toTable(cells);
faces={};
for rec in table:
	if faces.get(rec["Name"])==None:
		faces[rec["Name"]]=len(faces)+1;
	rec["Face"]=str(faces[rec["Name"]]);
	rec["Symbol"]=genSym(rec["Name"]);
	
cells=toCsv(table)

f=open('CardInfo.csv', 'w');
f.write("\xEF\xBB\xBF");
f.writelines([",".join(i)+"\n" for i in cells]);
f.close();

lines=[];

lines.append("namespace Assets.Data{");
lines.append("	public static class CardFace{");
for face in faces:
	lines.append("\t\t/** "+face+" */");
	lines.append("\t\tpublic const int CF_"+genSym(face)+" = "+str(faces[face])+";");
lines.append("	}");	
lines.append("}");	
f=open('Data/CardFace.cs','w');
f.write("\n".join(lines));
f.close();


