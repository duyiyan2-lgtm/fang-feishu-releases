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
          50: '#E8F0FF',
          100: '#D0E1FF',
          200: '#A8C5FF',
          soft: 'rgba(51, 112, 255, 0.08)',
          softHover: 'rgba(51, 112, 255, 0.14)'
        },
        sidebar: {
          DEFAULT: '#0F1115',
          hover: '#1A1D24',
          active: '#3370FF',
          dark: '#0A0C10',
          muted: '#8B929E'
        },
        surface: {
          DEFAULT: '#FFFFFF',
          secondary: '#F5F6F7',
          tertiary: '#EEF0F3',
          elevated: '#FFFFFF'
        },
        ink: {
          DEFAULT: '#1F2329',
          secondary: '#646A73',
          tertiary: '#8F959E',
          inverse: '#FFFFFF'
        },
        line: {
          DEFAULT: '#DEE0E3',
          soft: '#E8EAED',
          strong: '#C9CDD4'
        },
        bg: {
          DEFAULT: '#F2F3F5',
          card: '#FFFFFF'
        }
      },
      fontFamily: {
        sans: [
          '"PingFang SC"',
          '"Microsoft YaHei UI"',
          '"Microsoft YaHei"',
          'Inter',
          'system-ui',
          '-apple-system',
          'BlinkMacSystemFont',
          '"Segoe UI"',
          'sans-serif'
        ]
      },
      fontSize: {
        '2xs': ['11px', { lineHeight: '16px' }],
        'xs': ['12px', { lineHeight: '18px' }],
        'sm': ['13px', { lineHeight: '20px' }],
        'base': ['14px', { lineHeight: '22px' }],
        'lg': ['16px', { lineHeight: '24px' }],
        'xl': ['18px', { lineHeight: '26px' }],
        '2xl': ['22px', { lineHeight: '30px' }]
      },
      boxShadow: {
        'soft': '0 1px 2px rgba(31, 35, 41, 0.04), 0 2px 8px rgba(31, 35, 41, 0.06)',
        'card': '0 2px 12px rgba(31, 35, 41, 0.08)',
        'float': '0 8px 24px rgba(31, 35, 41, 0.12)',
        'dialog': '0 12px 40px rgba(31, 35, 41, 0.16)',
        'glow': '0 0 0 3px rgba(51, 112, 255, 0.18)',
        'sidebar': '2px 0 12px rgba(0, 0, 0, 0.06)'
      },
      borderRadius: {
        'xs': '4px',
        'sm': '6px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
        '2xl': '20px'
      },
      spacing: {
        '13': '3.25rem',
        '15': '3.75rem',
        '18': '4.5rem',
        '22': '5.5rem'
      },
      transitionTimingFunction: {
        'smooth': 'cubic-bezier(0.22, 1, 0.36, 1)',
        'snappy': 'cubic-bezier(0.2, 0, 0, 1)'
      },
      transitionDuration: {
        '120': '120ms',
        '180': '180ms',
        '250': '250ms',
        '320': '320ms'
      },
      keyframes: {
        'fade-in': {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' }
        },
        'slide-up': {
          '0%': { opacity: '0', transform: 'translateY(8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        },
        'slide-in-right': {
          '0%': { opacity: '0', transform: 'translateX(12px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' }
        },
        'scale-in': {
          '0%': { opacity: '0', transform: 'scale(0.96)' },
          '100%': { opacity: '1', transform: 'scale(1)' }
        },
        'pulse-soft': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.55' }
        },
        'shimmer': {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' }
        }
      },
      animation: {
        'fade-in': 'fade-in 180ms ease-out',
        'slide-up': 'slide-up 220ms cubic-bezier(0.22, 1, 0.36, 1)',
        'slide-in-right': 'slide-in-right 220ms cubic-bezier(0.22, 1, 0.36, 1)',
        'scale-in': 'scale-in 180ms cubic-bezier(0.22, 1, 0.36, 1)',
        'pulse-soft': 'pulse-soft 1.6s ease-in-out infinite',
        'shimmer': 'shimmer 1.4s linear infinite'
      },
      backdropBlur: {
        xs: '2px'
      }
    }
  },
  plugins: []
}
