import type { CapacitorConfig } from '@capacitor/cli'

const isDev = process.env.NODE_ENV !== 'production'

const config: CapacitorConfig = {
  appId: 'org.misbahulhoda.app',
  appName: 'Misbah ul Hoda',
  webDir: '../web/dist',
  server: isDev
    ? {
        url: 'http://192.168.100.230:5173',
        cleartext: true,
      }
    : undefined,
  ios: {
    contentInset: 'automatic',
    backgroundColor: '#010f05',
    allowsLinkPreview: false,
    scrollEnabled: true,
  },
  android: {
    backgroundColor: '#010f05',
    allowMixedContent: true,
  },
}

export default config
