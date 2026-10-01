import { useEffect, useState } from 'react';
import axios from 'axios';

// State tanımlamaları
const [brands, setBrands] = useState<{ id: number; name: string }[]>([]);
const [seriesList, setSeriesList] = useState<{ id: number; name: string }[]>([]);
const [trimList, setTrimList] = useState<{ id: number; name: string }[]>([]);

const [selectedBrand, setSelectedBrand] = useState<number | ''>('');
const [selectedSeries, setSelectedSeries] = useState<number | ''>('');
const [selectedTrim, setSelectedTrim] = useState<number | ''>('');

// 1. Markaları Getir
useEffect(() => {
  axios.get('/api/vehicles/brands?vehicleType=Otomobil')
    .then(res => setBrands(res.data))
    .catch(err => console.error(err));
}, []);

// 2. Marka Seçilince Serileri Getir
const handleBrandChange = (brandId: number) => {
  setSelectedBrand(brandId);
  setSelectedSeries('');
  setSelectedTrim('');
  setSeriesList([]);
  setTrimList([]);

  axios.get(`/api/vehicles/series/${brandId}`)
    .then(res => setSeriesList(res.data));
};

// 3. Seri Seçilince Paket/Motorları Getir
const handleSeriesChange = (seriesId: number) => {
  setSelectedSeries(seriesId);
  setSelectedTrim('');
  setTrimList([]);

  axios.get(`/api/vehicles/trims/${seriesId}`)
    .then(res => setTrimList(res.data));
};