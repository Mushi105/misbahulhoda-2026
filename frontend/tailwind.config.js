/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50:  '#fffdf0',
          100: '#fff8d6',
          200: '#fef0a0',
          300: '#f5d842',
          400: '#e8b800',
          500: '#D4A800',
          600: '#a67800',
          700: '#7a5800',
          800: '#4a3400',
          900: '#2d2000',
        },
        emerald: {
          50:  '#fffdf0',
          100: '#fff8d6',
          200: '#fde68a',
          300: '#f5c842',
          400: '#D4A800',
          500: '#a67800',
          600: '#7a5800',
          700: '#5c4000',
          800: '#1a1400',
          900: '#0f0c00',
          950: '#080600',
        },
        gold: {
          50:  '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#d97706',
          600: '#b45309',
          700: '#92400e',
          800: '#78350f',
          900: '#451a03',
        },
        dark: {
          50:  '#f8fafc',
          100: '#f1f5f9',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617',
        }
      },
      fontFamily: {
        arabic: ['Amiri', 'serif'],
        urdu:   ['Noto Nastaliq Urdu', 'Amiri', 'serif'],
        sans:   ['Inter', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
