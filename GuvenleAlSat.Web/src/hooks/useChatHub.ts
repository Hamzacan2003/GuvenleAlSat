import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

export interface ChatMessagePayload {
  conversationId: string;
  senderId: string;
  content: string;
  sentAt: string;
}

export const useChatHub = () => {
  const [messages, setMessages] = useState<ChatMessagePayload[]>([]);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5121/hubs/chat', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (msg: ChatMessagePayload) => {
      setMessages((prev) => [...prev, msg]);
    });

    connection.on('MessageSent', (msg: ChatMessagePayload) => {
      setMessages((prev) => [...prev, msg]);
    });

    connection.start().catch((err) => console.error('SignalR Bağlantı Hatası: ', err));
    hubRef.current = connection;

    return () => {
      connection.stop();
    };
  }, []);

  const sendMessage = async (listingId: string, receiverId: string, content: string) => {
    if (hubRef.current && hubRef.current.state === signalR.HubConnectionState.Connected) {
      await hubRef.current.invoke('SendMessage', listingId, receiverId, content);
    }
  };

  return { messages, sendMessage };
};