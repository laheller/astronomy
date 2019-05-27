/*
    MIT License

    Copyright (c) 2019 Don Cross <cosinekitty@gmail.com>

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

const fs = require('fs');
const xml2js = require('xml2js');

function Find(m, key) {
    for (let e of m.$$) {
        if (e['#name'] === key) {
            return e;
        }
    }
    throw `Find: could not find key "${key}" in:\n${JSON.stringify(m,null,2)}`;
}

function KeyText(m, key) {
    return Find(m, key)._;
}

function MemberId(m) {
    return m.$.id;
}

function MemberName(m) {
    return KeyText(m, 'name');
}

function MemberExpansion(m) {
    return Find(m, 'initializer').$$[0]._;
}

function MemberDetail(m) {
    return Find(m, 'detaileddescription').para[0].$$;
}

function FlatText(x) {
    if (typeof x === 'string') {
        return x;
    } 

    if (x instanceof Array) {
        let s = '';
        for (let e of x) {
            s += FlatText(e);
        }
        return s;
    }     
    
    if (typeof x === 'object') {
        if (x.$$) {
            return FlatText(x.$$);
        }

        if (x._) {
            return FlatText(x._);
        }
    }

    throw `FlatText: don't know how to convert: ${x}`;
}

class Define {
    constructor(m) {
        this.id = MemberId(m);
        this.name = MemberName(m);
        this.expansion = MemberExpansion(m);
        this.detail = MemberDetail(m);
        console.log(`    #define ${this.name} ${this.expansion}  // ${FlatText(this.detail)} // ${this.id}`);
    }
}

class Parm {
    constructor(name, type) {
        this.name = name;
        this.type = type;
    }
}

class FuncInfo {
    constructor(name, type, argtext, parmlist) {
        this.name = name;
        this.type = type;
        this.argtext = argtext;
        this.parmlist = parmlist;
    }
}

class Transformer {
    constructor(sectlist) {
        for (let sect of sectlist) {
            switch (sect.$.kind) {
            case 'define':
                this.defines = this.ParseDefineList(sect.memberdef);
                break;
            case 'enum':
                this.enums = this.ParseEnumList(sect.memberdef);
                break;
            case 'typedef':
                this.typedefs = this.ParseTypesdefList(sect.memberdef);
                break;
            case 'func':
                this.funcs = this.ParseFuncList(sect.memberdef);
                break;
            default:
                console.log(`hydrogen: ignoring "${sect.$.kind}"`);
                break;
            }
        }
    }

    ParseDefineList(mlist) {
        console.log(`hydrogen: processing ${mlist.length} defines`);
        let dlist = [];
        for (let m of mlist) {
            dlist.push(new Define(m));
        }
        return dlist;
    }

    ParseEnumList(mlist) {
        console.log(`hydrogen: processing ${mlist.length} enums`);
        let elist = [];
        return elist;
    }

    ParseTypesdefList(mlist) {
        console.log(`hydrogen: processing ${mlist.length} typedefs`);
        let tlist = [];
        return tlist;
    }

    ParseFuncList(mlist) {
        console.log(`hydrogen: processing ${mlist.length} funcs`);
        let flist = [];
        return flist;
    }

    Render() {
        return '';
    }
}

function run(headerXmlFileName, markdownFileName) {
    const headerXml = fs.readFileSync(headerXmlFileName);
    const parser = new xml2js.Parser({
        explicitChildren: true,
        preserveChildrenOrder: true,
        charsAsChildren: true
    });

    parser.parseString(headerXml, function(err, result) {
        const sectlist = result.doxygen.compounddef[0].sectiondef;
        //const dump = JSON.stringify(sectlist, null, 2);
        //fs.writeFileSync('dump.json', dump);
        const xform = new Transformer(sectlist);
        const markdown = xform.Render();
        fs.writeFileSync(markdownFileName, markdown);
    });
}

if (process.argv.length === 3) {
    run(process.argv[2], 'README.md');
    process.exit(0);
} else {
    console.log('USAGE: node hydrogen.js header.xml');
    process.exit(1);
}
