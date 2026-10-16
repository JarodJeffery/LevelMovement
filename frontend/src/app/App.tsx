import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Link, Navigate, Route, Routes } from 'react-router-dom';
import { VehicleListPage } from '../features/vehicles/views/VehicleListPage';
import { VehicleEditPage } from '../features/vehicles/views/VehicleEditPage';
const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
export default function App() {
  return <QueryClientProvider client={client}><BrowserRouter><header className="site-header"><Link to="/vehicles" className="brand"><img className="brand-logo" src="/branding/agilebridge-logo.png" alt="AgileBridge" /><span>Level Movement<small>Vehicle management</small></span></Link><span className="environment">Fleet workspace</span></header><main><Routes><Route path="/vehicles" element={<VehicleListPage />} /><Route path="/vehicles/new" element={<VehicleEditPage key="new" create />} /><Route path="/vehicles/:registration" element={<VehicleEditPage />} /><Route path="*" element={<Navigate to="/vehicles" replace />} /></Routes></main><footer>Level Movement · Vehicle hire</footer></BrowserRouter></QueryClientProvider>;
}
