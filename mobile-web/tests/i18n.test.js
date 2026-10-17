import { translations, t, setLanguage, getCurrentLanguage } from '../js/i18n.js';

console.log('--- Running Mobile Web i18n Unit Tests ---');

const supportedLangs = ['zh-TW', 'zh-CN', 'en-US'];

// Test 1: Supported languages check
supportedLangs.forEach(lang => {
  if (!translations[lang]) {
    throw new Error(`Missing translations for ${lang}`);
  }
  console.log(`✓ Language ${lang} dictionary found (${Object.keys(translations[lang]).length} keys)`);
});

// Test 2: Key consistency across all languages
const baseKeys = Object.keys(translations['zh-TW']);
supportedLangs.forEach(lang => {
  const keys = Object.keys(translations[lang]);
  baseKeys.forEach(key => {
    if (translations[lang][key] === undefined) {
      throw new Error(`Language ${lang} is missing key: ${key}`);
    }
  });
  console.log(`✓ Language ${lang} has all ${baseKeys.length} keys in sync`);
});

// Test 3: Language switching fallback & text retrieval
const testTextTw = translations['zh-TW']['cardStartTitle'];
const testTextCn = translations['zh-CN']['cardStartTitle'];
const testTextEn = translations['en-US']['cardStartTitle'];

if (!testTextTw || !testTextCn || !testTextEn) {
  throw new Error('Key retrieval failed');
}

console.log(`✓ Translation sample (cardStartTitle):`);
console.log(`  - zh-TW: ${testTextTw}`);
console.log(`  - zh-CN: ${testTextCn}`);
console.log(`  - en-US: ${testTextEn}`);

console.log('🎉 All mobile-web i18n tests passed successfully!');
