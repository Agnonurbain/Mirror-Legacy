import React from 'react';
import { Coins, Star, Sparkles, Calendar, Hexagon } from 'lucide-react';
import { Clan } from '../types/game';
import { Tooltip } from './ui/Tooltip';

interface TopBarProps {
  clan: Clan;
}

export const TopBar: React.FC<TopBarProps> = ({ clan }) => {
  return (
    <div className="h-20 bg-[#0a0a0a] border-b border-amber-900/30 flex items-center justify-between px-8 sticky top-0 z-50 shadow-[0_4px_20px_rgba(0,0,0,0.5)] relative overflow-hidden">
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      <div className="absolute inset-x-0 bottom-0 h-px bg-gradient-to-r from-transparent via-amber-500/50 to-transparent" />
      
      <div className="relative z-10 flex flex-col justify-center">
        <div className="flex items-center gap-4">
          <h2 className="text-2xl font-calligraphy text-amber-500 flex items-center gap-4 drop-shadow-[0_0_10px_rgba(245,158,11,0.3)]">
            {clan.name}
          </h2>
          <Tooltip content="Le Rang du clan détermine votre influence globale. Augmentez votre Renommée pour monter en rang." position="bottom">
            <span className="text-[10px] font-mono px-3 py-1 bg-amber-900/20 text-amber-400 border border-amber-500/30 rounded-full uppercase tracking-[0.2em] shadow-[0_0_10px_rgba(245,158,11,0.1)] cursor-help">
              Rang {clan.tier}
            </span>
          </Tooltip>
        </div>
        <p className="text-xs italic font-serif mt-1 tracking-wide text-amber-100/50">"{clan.motto}"</p>
      </div>

      <div className="flex items-center space-x-6 relative z-10">
        <Tooltip content={
          <div className="space-y-1">
            <p className="font-bold text-cyan-400">Énergie Divine</p>
            <p className="text-stone-400">L'énergie accumulée dans le Miroir de Bronze. S'obtient passivement chaque année et lors des percées de vos disciples. Permet d'utiliser des Pouvoirs Divins.</p>
          </div>
        } position="bottom">
          <div className="flex items-center space-x-3 bg-black/40 px-5 py-2 rounded-2xl border border-amber-900/30 group hover:border-cyan-500/50 transition-all relative overflow-hidden cursor-help shadow-inner">
            <div className="absolute bottom-0 left-0 h-1 bg-cyan-500/50 transition-all duration-1000 shadow-[0_0_10px_rgba(6,182,212,0.8)]" style={{ width: `${clan.mirrorPower || 0}%` }} />
            <Hexagon size={20} className="text-cyan-400 opacity-80 group-hover:opacity-100 transition-colors drop-shadow-[0_0_5px_rgba(6,182,212,0.5)]" />
            <div className="flex flex-col relative z-10">
              <span className="text-[9px] uppercase tracking-[0.2em] opacity-50 font-bold text-cyan-400">Miroir</span>
              <span className="font-mono text-sm font-bold text-cyan-400">{Math.floor(clan.mirrorPower || 0)}/100</span>
            </div>
          </div>
        </Tooltip>

        <Tooltip content="L'année actuelle de votre clan. Avancez le temps depuis la Chronique." position="bottom">
          <div className="flex items-center space-x-3 bg-black/40 px-5 py-2 rounded-2xl border border-amber-900/30 group hover:border-amber-500/50 transition-all cursor-help shadow-inner">
            <Calendar size={20} className="text-amber-700/50 group-hover:text-amber-500 transition-colors" />
            <div className="flex flex-col">
              <span className="text-[9px] uppercase tracking-[0.2em] text-amber-700/50 font-bold">Ère</span>
              <span className="font-mono text-sm font-bold text-amber-100 group-hover:text-amber-400 transition-colors">{clan.currentYear}</span>
            </div>
          </div>
        </Tooltip>

        <Tooltip content={
          <div className="space-y-1">
            <p className="font-bold text-amber-400">Pierres Spirituelles (Trésor)</p>
            <p className="text-stone-400">La monnaie du monde de la culture. Utilisée pour améliorer les Pavillons, forger, ou interagir avec d'autres sectes.</p>
          </div>
        } position="bottom">
          <div className="flex items-center space-x-3 bg-black/40 px-5 py-2 rounded-2xl border border-amber-900/30 group hover:border-amber-500/50 transition-all cursor-help shadow-inner">
            <Coins size={20} className="text-amber-500 opacity-80 group-hover:opacity-100 transition-colors drop-shadow-[0_0_5px_rgba(245,158,11,0.5)]" />
            <div className="flex flex-col">
              <span className="text-[9px] uppercase tracking-[0.2em] text-amber-700/50 font-bold">Trésor</span>
              <span className="font-mono text-sm font-bold text-amber-400">{clan.wealth.toLocaleString()}</span>
            </div>
          </div>
        </Tooltip>

        <Tooltip content={
          <div className="space-y-1">
            <p className="font-bold text-emerald-400">Renommée</p>
            <p className="text-stone-400">La réputation de votre clan dans le monde martial. Une haute renommée permet de monter en Rang et attire le respect des autres sectes.</p>
          </div>
        } position="bottom">
          <div className="flex items-center space-x-3 bg-black/40 px-5 py-2 rounded-2xl border border-amber-900/30 group hover:border-emerald-500/50 transition-all cursor-help shadow-inner">
            <Star size={20} className="text-emerald-500 opacity-80 group-hover:opacity-100 transition-colors drop-shadow-[0_0_5px_rgba(16,185,129,0.5)]" />
            <div className="flex flex-col">
              <span className="text-[9px] uppercase tracking-[0.2em] text-amber-700/50 font-bold">Renommée</span>
              <span className="font-mono text-sm font-bold text-emerald-400">{clan.prestige.toLocaleString()}</span>
            </div>
          </div>
        </Tooltip>

        <Tooltip content={
          <div className="space-y-1">
            <p className="font-bold text-amber-400">Destinée</p>
            <p className="text-stone-400">La providence céleste accordée à votre clan. Augmente les chances de succès de toutes les actions (percées, artisanat, rencontres). S'obtient lors du décès de grands cultivateurs ou d'événements majeurs.</p>
          </div>
        } position="bottom">
          <div className="flex items-center space-x-3 bg-black/40 px-5 py-2 rounded-2xl border border-amber-900/30 group hover:border-amber-500/50 transition-all cursor-help shadow-inner">
            <Sparkles size={20} className="text-amber-500 opacity-80 group-hover:opacity-100 transition-colors drop-shadow-[0_0_5px_rgba(245,158,11,0.5)]" />
            <div className="flex flex-col">
              <span className="text-[9px] uppercase tracking-[0.2em] text-amber-700/50 font-bold">Destinée</span>
              <span className="font-mono text-sm font-bold text-amber-400">{clan.destiny}</span>
            </div>
          </div>
        </Tooltip>
      </div>
    </div>
  );
};
