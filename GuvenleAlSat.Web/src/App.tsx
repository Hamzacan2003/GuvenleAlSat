import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Header } from './components/Header';
import { HomePage } from './pages/HomePage';
import { ListingDetailPage } from './pages/ListingDetailPage';
import { CreateListingPage } from './pages/CreateListingPage';
import { MyListingsPage } from './pages/MyListingsPage';
import { MessagesPage } from './pages/MessagesPage';
import { EditListingPage } from './pages/EditListingPage';
import { ProfilePage } from './pages/ProfilePage';

function App() {
  return (
    <Router>
      <div className="min-h-screen bg-[#f7f7f7] flex flex-col font-sans">
        <Header />
        <main className="flex-1">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/ilan/:listingNo" element={<ListingDetailPage />} />
            <Route path="/ilan-ver" element={<CreateListingPage />} />
            <Route path="/bana-ozel/ilanlarim" element={<MyListingsPage />} />
            <Route path="/bana-ozel/mesajlarim" element={<MessagesPage />} />
            <Route path="/profilim" element={<ProfilePage />} />
            <Route path="/ilan-duzenle/:listingNo" element={<EditListingPage />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;