import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Link } from 'react-router-dom';
import { Send, MessageSquare, RefreshCw, ExternalLink } from 'lucide-react';

export const MessagesPage: React.FC = () => {
  const [conversations, setConversations] = useState<any[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<any>(null);
  const [chatMessages, setChatMessages] = useState<any[]>([]);
  const [messageText, setMessageText] = useState('');
  const [loading, setLoading] = useState(true);

  const getCurrentUserId = (): string | null => {
    try {
      const token = localStorage.getItem('accessToken') || localStorage.getItem('token');
      if (!token || !token.includes('.')) return null;
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(decodeURIComponent(escape(window.atob(base64))));
      return (
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
        payload['sub'] ||
        payload['id'] ||
        null
      );
    } catch {
      return null;
    }
  };

  const currentUserId = getCurrentUserId();

  const loadConversations = async () => {
    setLoading(true);
    try {
      const res = await api.get('/messages/conversations');
      if (res?.data?.success && Array.isArray(res.data.data)) {
        setConversations(res.data.data);
        if (res.data.data.length > 0 && !selectedConversation) {
          handleSelectConversation(res.data.data[0]);
        }
      } else {
        setConversations([]);
      }
    } catch (err) {
      console.error('Konuşmalar alınamadı:', err);
      setConversations([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadConversations();
  }, []);

  // Konuşma seçildiğinde hem mesajları getir hem de bildirimi (okundu) sıfırla
  const handleSelectConversation = async (conv: any) => {
    setSelectedConversation(conv);

    // 1. Ekrandaki kırmızı sayacı hemen kaldır
    setConversations((prev) =>
      prev.map((c) => (c.id === conv.id ? { ...c, unreadCount: 0 } : c))
    );

    // 2. Backend'e okundu bilgisini gönder
    try {
      await api.post('/messages/mark-read', { conversationId: conv.id });
    } catch {}

    // 3. Mesajları yükle
    try {
      const res = await api.get(
        `/messages/conversation?otherUserId=${conv.otherUserId}&listingId=${conv.listingId}`
      );
      if (res?.data?.success && Array.isArray(res.data.data)) {
        setChatMessages(res.data.data);
      } else {
        setChatMessages([]);
      }
    } catch {
      setChatMessages([]);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageText.trim() || !selectedConversation) return;

    const textToSend = messageText.trim();
    setMessageText('');

    try {
      const payload = {
        receiverId: selectedConversation.otherUserId,
        listingId: selectedConversation.listingId,
        content: textToSend,
      };

      await api.post('/messages', payload);

      setChatMessages((prev) => [
        ...prev,
        {
          id: Date.now().toString(),
          senderId: currentUserId,
          content: textToSend,
          sentAt: new Date().toISOString(),
        },
      ]);
    } catch {
      alert('Mesaj gönderilemedi, lütfen tekrar deneyin.');
    }
  };

  return (
    <div className="max-w-[1200px] mx-auto px-3 py-6 text-xs">
      <div className="flex justify-between items-center border-b pb-3 mb-4 bg-white p-4 rounded border">
        <div>
          <h1 className="text-base font-bold text-gray-900">Bana Özel &gt; Mesajlarım</h1>
          <p className="text-gray-500 text-[11px] mt-0.5">İlanlarınızla ilgili gelen ve gönderilen tüm mesajlar.</p>
        </div>
        <button
          onClick={loadConversations}
          className="flex items-center gap-1.5 bg-gray-100 hover:bg-gray-200 text-gray-700 px-3 py-1.5 rounded font-bold transition"
        >
          <RefreshCw className="w-3.5 h-3.5" />
          Yenile
        </button>
      </div>

      {loading ? (
        <div className="text-center py-20 bg-white border rounded text-gray-500 font-bold">
          Mesajlarınız yükleniyor...
        </div>
      ) : conversations.length === 0 ? (
        <div className="text-center py-20 bg-white border rounded shadow-2xs space-y-3">
          <div className="w-14 h-14 bg-blue-50 text-[#0055b8] rounded-full flex items-center justify-center mx-auto">
            <MessageSquare className="w-6 h-6" />
          </div>
          <p className="font-bold text-gray-800 text-sm">Gelen kutunuz boş.</p>
          <p className="text-gray-500 text-xs">Henüz bir ilan için mesaj almadınız veya göndermediniz.</p>
        </div>
      ) : (
        <div className="grid grid-cols-12 bg-white border rounded shadow-2xs h-[560px] overflow-hidden">
          {/* SOL: Konuşma Listesi */}
          <div className="col-span-4 border-r overflow-y-auto divide-y bg-gray-50/50">
            {conversations.map((c) => {
              const isSelected = selectedConversation?.id === c.id;
              return (
                <button
                  key={c.id}
                  onClick={() => handleSelectConversation(c)}
                  className={`w-full text-left p-3 flex gap-3 transition border-l-4 ${
                    isSelected
                      ? 'bg-blue-50/80 border-[#0055b8]'
                      : 'border-transparent hover:bg-gray-100/70'
                  }`}
                >
                  <div className="w-10 h-10 rounded-full bg-[#0055b8] text-white flex items-center justify-center font-bold text-sm shrink-0">
                    {c.otherUserName ? c.otherUserName[0].toUpperCase() : 'U'}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex justify-between items-center mb-0.5">
                      <span className="font-bold text-gray-900 truncate text-xs">{c.otherUserName}</span>
                      {c.unreadCount > 0 && (
                        <span className="bg-red-600 text-white text-[10px] px-1.5 py-0.2 rounded-full font-black animate-pulse">
                          {c.unreadCount}
                        </span>
                      )}
                    </div>
                    <div className="text-gray-600 text-[11px] font-semibold truncate">
                      İlan: #{c.listingNo} - {c.listingTitle}
                    </div>
                    <div className="text-gray-400 text-[10px] truncate mt-0.5">
                      {c.lastMessage || 'Mesaj yok'}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>

          {/* SAĞ: Aktif Sohbet Alanı */}
          <div className="col-span-8 flex flex-col h-full bg-[#f9fafb]">
            {selectedConversation ? (
              <>
                {/* ÜST BİLGİ & İLANA GİT BUTONU */}
                <div className="p-3 bg-white border-b flex justify-between items-center shadow-2xs">
                  <div>
                    <span className="font-bold text-gray-900 text-sm">
                      {selectedConversation.otherUserName}
                    </span>
                    <span className="text-gray-500 text-[11px] ml-2">
                      (İlan #{selectedConversation.listingNo}: {selectedConversation.listingTitle})
                    </span>
                  </div>

                  {/* İLGİLİ İLANA GİT BUTONU */}
                  <Link
                    to={`/ilan/${selectedConversation.listingNo}`}
                    target="_blank"
                    className="inline-flex items-center gap-1.5 bg-[#ffe100] hover:bg-[#ebd000] text-[#111] font-bold px-3 py-1.5 rounded text-[11px] border border-yellow-400 shadow-xs transition"
                  >
                    <span>İlana Git</span>
                    <ExternalLink className="w-3.5 h-3.5" />
                  </Link>
                </div>

                {/* Mesaj Balonları */}
                <div className="flex-1 p-4 overflow-y-auto space-y-2.5">
                  {chatMessages.length === 0 ? (
                    <div className="text-center py-16 text-gray-400">Henüz mesaj geçmişi bulunmuyor.</div>
                  ) : (
                    chatMessages.map((m, index) => {
                      const isMe = m.senderId?.toLowerCase() === currentUserId?.toLowerCase();
                      return (
                        <div
                          key={m.id || index}
                          className={`max-w-[70%] p-3 rounded-lg text-xs leading-relaxed shadow-2xs ${
                            isMe
                              ? 'ml-auto bg-[#0055b8] text-white rounded-br-none'
                              : 'bg-white border border-gray-200 text-gray-800 rounded-bl-none'
                          }`}
                        >
                          <div>{m.content}</div>
                          <div
                            className={`text-[9px] mt-1 text-right ${
                              isMe ? 'text-blue-100' : 'text-gray-400'
                            }`}
                          >
                            {m.sentAt ? new Date(m.sentAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) : ''}
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>

                {/* Mesaj Yazma Çubuğu */}
                <form onSubmit={handleSendMessage} className="p-3 bg-white border-t flex gap-2">
                  <input
                    type="text"
                    placeholder="Cevabınızı buraya yazın..."
                    className="flex-1 border border-gray-300 p-2 rounded text-xs focus:outline-none focus:border-[#0055b8]"
                    value={messageText}
                    onChange={(e) => setMessageText(e.target.value)}
                  />
                  <button
                    type="submit"
                    disabled={!messageText.trim()}
                    className="bg-[#0055b8] hover:bg-[#004494] text-white px-5 rounded font-bold flex items-center gap-1.5 text-xs transition disabled:opacity-50"
                  >
                    <Send className="w-3.5 h-3.5" />
                    Gönder
                  </button>
                </form>
              </>
            ) : (
              <div className="flex items-center justify-center h-full text-gray-400">
                Lütfen soldan bir konuşma seçin.
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};