/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#3370FF',
          hover: '#2860E0',
          active: '#1E50D0',
          50: '#E8EFFC',
          100: '#D6E2FE'
        },
        sidebar: {
          DEFAULT: '#1F2329',
          hover: '#2A2F37',
          active: '#3370FF',
          dark: '#14171C'
        },
        bg: {
          DEFAULT: '#F5F6F7',
          card: '#FFFFFF'
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      }
    }
  },
  plugins: []
}
