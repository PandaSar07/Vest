const fs = require('fs');
const glob = require('glob');
const files = glob.sync('src/components/**/*.tsx');

files.forEach(file => {
  let content = fs.readFileSync(file, 'utf8');
  let changed = false;

  // import currencySymbol
  if (content.includes('fmtMoney') && !content.includes('currencySymbol')) {
    content = content.replace(/fmtMoney([ \n\r,]*)} from '@\/lib\/format'/, 'fmtMoney, currencySymbol$1} from \'@/lib/format\'');
    changed = true;
  }

  // replace $${fmtMoney with ${currencySymbol}${fmtMoney
  if (content.includes('$${fmtMoney')) {
    content = content.replace(/\$\$\{fmtMoney/g, '${currencySymbol}${fmtMoney');
    changed = true;
  }
  
  // replace >$${fmtMoney with >${currencySymbol}${fmtMoney
  if (content.includes('>$${fmtMoney')) {
    content = content.replace(/>\$\$\{fmtMoney/g, '>${currencySymbol}${fmtMoney');
    changed = true;
  }

  if (changed) {
    fs.writeFileSync(file, content);
  }
});
console.log('Done');
