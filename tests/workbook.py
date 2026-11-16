"""Compare all original tables, columns and rows with import.xlsx using SQL Server."""
import datetime
import pathlib
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

database = sys.argv[1]
if not database.startswith('emo_validation_'):
    raise ValueError('An isolated emo_validation_ database is required')
n = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(pathlib.Path(__file__).resolve().parents[1] / 'import.xlsx') as z:
    strings = [''.join(e.itertext()) for e in ET.fromstring(z.read('xl/sharedStrings.xml'))]
    relations = {e.attrib['Id']: e.attrib['Target'] for e in ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
    sql = ['SET NOCOUNT ON;']
    for sheet in ET.fromstring(z.read('xl/workbook.xml')).find('s:sheets', n):
        name = sheet.attrib['name']
        target = relations[sheet.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']]
        rows = ET.fromstring(z.read('xl/' + target)).findall('s:sheetData/s:row', n)
        def value(cell):
            v = cell.find('s:v', n)
            return strings[int(v.text)] if cell.get('t') == 's' else v.text if v is not None else ''
        headers = [value(c) for c in rows[0]]
        cols = ','.join('[' + h + ']' for h in headers)
        sql.append(f"IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'{name}')) <> {len(headers)} THROW 51000, '{name}: column count mismatch', 1;")
        for i, h in enumerate(headers, 1):
            sql.append(f"IF COL_NAME(OBJECT_ID(N'{name}'), {i}) <> N'{h}' THROW 51000, '{name}: column name mismatch', 1;")
        expected = []
        for row in rows[1:]:
            cells = list(row)
            if not cells or not value(cells[0]):
                continue
            vals = []
            for h, c in zip(headers, cells):
                v = value(c)
                if h == 'Дата заказа':
                    v = (datetime.datetime(1899, 12, 30) + datetime.timedelta(days=float(v))).isoformat()
                vals.append("N'" + v.replace("'", "''") + "'")
            expected.append('(' + ','.join(vals) + ')')
        sql.append(f"IF (SELECT COUNT(*) FROM [{name}]) <> {len(expected)} THROW 51000, '{name}: row count mismatch', 1;")
        sql.append(f"IF EXISTS (SELECT {cols} FROM (VALUES {','.join(expected)}) v({cols}) EXCEPT SELECT {cols} FROM [{name}]) THROW 51000, '{name}: data mismatch', 1;")
    sql.append("PRINT 'PASS all ten Excel sheets: exact columns, row counts and values';")
with tempfile.TemporaryDirectory() as folder:
    path = pathlib.Path(folder) / 'verify.sql'
    path.write_text('\n'.join(sql), encoding='utf-8-sig')
    subprocess.run(['sqlcmd', '-S', 'localhost', '-E', '-C', '-b', '-d', database, '-i', str(path)], check=True)
