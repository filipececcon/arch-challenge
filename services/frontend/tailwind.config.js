/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        navy: {
          950: '#08111e',
          900: '#0d1b2a',
          800: '#0f2133',
          700: '#152c44',
          600: '#1e3a5f',
        },
        brand: {
          DEFAULT: '#4361ee',
          light: '#4cc9f0',
        },
      },
    },
  },
  plugins: [],
}

