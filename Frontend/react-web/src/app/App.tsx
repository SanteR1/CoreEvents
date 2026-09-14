import { Outlet, useNavigation } from 'react-router';
import { ThemeProvider } from '@/shared/lib/theme';
import { Header } from '@/widgets/header';

export default function App() {
  const navigation = useNavigation();
  const isLoading = navigation.state === 'loading';

  return (
    <ThemeProvider>
      <div className="min-h-screen bg-(--bg) text-(--text) transition-colors duration-200">
        <Header />
        <main
          className="mx-auto max-w-7xl p-6 transition-opacity duration-200"
          style={{ opacity: isLoading ? 0.6 : 1 }}
        >
          <Outlet />
        </main>
      </div>
    </ThemeProvider>
  );
}
