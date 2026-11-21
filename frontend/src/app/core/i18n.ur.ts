export const ur = {
  'nav.properties': 'Properties',
  'nav.post': 'Post Property',
  'nav.map': 'Map',
  'nav.tools': 'Tools',
  'nav.explore': 'Explore',
  'nav.projects': 'New Projects',
  'nav.agencies': 'Agencies',
  'nav.dashboard': 'Dashboard',
  'nav.saved': 'Saved Searches',
  'nav.leads': 'Leads',
  'nav.reports': 'Reports',
  'auth.login': 'Login',
  'auth.logout': 'Logout',
  'notif.title': 'Notifications',
  'notif.markAll': 'Mark all read',
  'notif.empty': 'Abhi koi notification nahi.',
  'footer.tagline': 'Pakistani property market ke liye smart property decisions.',
  'footer.properties': 'Properties',
  'footer.post': 'Post Property',
  'footer.agentLogin': 'Agent Login',
  'pwa.install': 'App install karein',
  'lang.switch': 'English'
} as const;

export type TranslationKey = keyof typeof ur;
