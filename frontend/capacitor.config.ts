import type { CapacitorConfig } from '@capacitor/cli'

const config: CapacitorConfig = {
  appId: 'org.misbahulhoda.app',
  appName: 'Misbah ul Hoda',
  webDir: 'dist',
  server: {
    url: 'http://192.168.100.230:5173',
    cleartext: true,
  },
  ios: {
    contentInset: 'automatic',
    backgroundColor: '#010f05',
    allowsLinkPreview: false,
    scrollEnabled: true,
  },
}

export default config
