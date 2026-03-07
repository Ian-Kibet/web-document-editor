// Set of Google Font family names (lowercase for matching)
const GOOGLE_FONTS = new Set([
  'arimo', 'carlito', 'tinos', 'cousine', 'caladea',
  'roboto', 'open sans', 'lato', 'merriweather',
  'noto sans', 'source sans pro', 'ubuntu', 'oswald',
  'pt sans', 'pt serif', 'raleway', 'nunito',
]);

// Track which fonts have already been injected
const _loaded = new Set<string>();

export function ensureFontsLoaded(fontFamilies: Iterable<string>): void {
  for (const family of fontFamilies) {
    const key = family.toLowerCase().trim();
    if (!_loaded.has(key) && GOOGLE_FONTS.has(key)) {
      _loaded.add(key);
      loadGoogleFont(family.trim());
    }
  }
}

function loadGoogleFont(family: string): void {
  // Inject preconnect links once
  if (!document.querySelector('link[data-gf-preconnect]')) {
    const pc1 = Object.assign(document.createElement('link'), {
      rel: 'preconnect', href: 'https://fonts.googleapis.com',
    });
    pc1.setAttribute('data-gf-preconnect', '');
    const pc2 = Object.assign(document.createElement('link'), {
      rel: 'preconnect', href: 'https://fonts.gstatic.com',
    });
    pc2.crossOrigin = 'anonymous';
    pc2.setAttribute('data-gf-preconnect', '');
    document.head.append(pc1, pc2);
  }

  // Load the font: regular + bold + italic + bold-italic
  const encoded = encodeURIComponent(family);
  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = `https://fonts.googleapis.com/css2?family=${encoded}:ital,wght@0,400;0,700;1,400;1,700&display=swap`;
  document.head.appendChild(link);
}
