import type { CapacitorConfig } from '@capacitor/cli'

const config: CapacitorConfig = {
  appId: 'com.misbahulhoda.arbaeen2026',
  appName: 'Misbah ul Hoda',
  webDir: 'dist',
  server: {
    url: 'https://misbahulhoda.mubashirhasan.dev',
    cleartext: false,
  },
  android: {
    backgroundColor: '#010f05',
    allowMixedContent: false,
  },
  ios: {
    contentInset: 'automatic',
    backgroundColor: '#010f05',
    allowsLinkPreview: false,
    scrollEnabled: true,
  },
}

export default config
