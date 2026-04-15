import React from 'react';
import { Clan, MarketItem } from '../../types/game';
import { ShoppingCart, Coins, Package, Book, PawPrint, Sparkles, Hexagon } from 'lucide-react';
import { motion } from 'motion/react';

interface MarketViewProps {
  clan: Clan;
  onBuyItem: (itemId: string) => void;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: {
    y: 0,
    opacity: 1,
    transition: { type: "spring", stiffness: 100 }
  }
};

export const MarketView: React.FC<MarketViewProps> = ({ clan, onBuyItem }) => {
  const marketItems = clan.marketItems || [];

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative"
    >
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      
      {/* Header Section */}
      <motion.div 
        variants={itemVariants}
        className="bg-[#140d0a] border border-amber-900/30 rounded-3xl p-8 md:p-12 shadow-2xl mb-12 relative overflow-hidden flex flex-col md:flex-row items-center justify-between gap-8"
      >
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-50" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-amber-900/40 to-black border border-amber-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(217,119,6,0.2)]">
            <ShoppingCart className="text-amber-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-amber-500 drop-shadow-md">Marché Céleste</h2>
            <p className="text-sm text-amber-700/70 font-mono uppercase tracking-[0.2em] mt-2">Commerce et Échanges</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-amber-900/50 shadow-inner">
          <span className="text-xs text-amber-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Coins size={14} className="text-amber-500" />
            Fonds Disponibles
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-amber-400 drop-shadow-[0_0_10px_rgba(251,191,36,0.5)]">
              {clan.wealth.toLocaleString()}
            </span>
            <span className="text-sm text-amber-700/50 font-serif italic">Pierres d'Esprit</span>
          </div>
        </div>
      </motion.div>

      {marketItems.length === 0 ? (
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-amber-900/30 rounded-3xl p-16 text-center relative overflow-hidden shadow-xl">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          <div className="relative z-10 flex flex-col items-center">
            <div className="w-24 h-24 rounded-full bg-black/50 border border-amber-900/50 flex items-center justify-center mb-6">
              <ShoppingCart className="w-12 h-12 text-amber-700/30" />
            </div>
            <h3 className="text-3xl font-calligraphy text-amber-500 mb-4">Le marché est vide</h3>
            <p className="text-amber-100/50 font-serif italic text-lg max-w-md">
              Les marchands itinérants ne sont pas encore arrivés. Revenez au prochain cycle pour découvrir de nouvelles offres.
            </p>
          </div>
        </motion.div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8 relative z-10">
          {marketItems.map((marketItem) => {
            const canAfford = clan.wealth >= marketItem.cost;
            const itemData = marketItem.type === 'Item' ? marketItem.item : 
                             marketItem.type === 'Manual' ? marketItem.manual : 
                             marketItem.beast;
            
            const Icon = marketItem.type === 'Item' ? Package : 
                         marketItem.type === 'Manual' ? Book : PawPrint;
                         
            const colorClass = marketItem.type === 'Item' ? 'text-blue-400' : 
                               marketItem.type === 'Manual' ? 'text-purple-400' : 'text-orange-400';
                               
            const bgClass = marketItem.type === 'Item' ? 'bg-blue-900/20' : 
                            marketItem.type === 'Manual' ? 'bg-purple-900/20' : 'bg-orange-900/20';
                            
            const borderClass = marketItem.type === 'Item' ? 'border-blue-900/30' : 
                                marketItem.type === 'Manual' ? 'border-purple-900/30' : 'border-orange-900/30';

            return (
              <motion.div 
                key={marketItem.id} 
                variants={itemVariants}
                whileHover={{ y: -5, scale: 1.02 }}
                className={`bg-[#1a120f] border rounded-3xl p-6 flex flex-col justify-between transition-all shadow-xl relative overflow-hidden group ${borderClass} hover:border-amber-500/50`}
              >
                {/* Decorative Elements */}
                <div className={`absolute top-0 right-0 w-32 h-32 bg-gradient-to-bl ${bgClass} to-transparent rounded-bl-full pointer-events-none opacity-50`} />
                <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                  <Icon size={120} />
                </div>

                <div className="relative z-10">
                  <div className="flex items-start justify-between mb-6">
                    <div className={`w-14 h-14 rounded-2xl bg-black/50 border shadow-inner flex items-center justify-center ${borderClass}`}>
                      <Icon size={28} className={colorClass} />
                    </div>
                    <div className="bg-black/40 px-3 py-1.5 rounded-lg border border-amber-900/30">
                      <span className="text-[9px] uppercase tracking-widest font-mono text-amber-700/70">
                        {marketItem.type === 'Item' ? 'Objet' : 
                         marketItem.type === 'Manual' ? 'Manuel' : 'Bête Spirituelle'}
                      </span>
                    </div>
                  </div>
                  
                  <h3 className="text-xl font-serif font-bold mb-3 text-amber-100 group-hover:text-amber-400 transition-colors">
                    {itemData?.name}
                  </h3>
                  
                  <p className="text-sm text-amber-100/60 leading-relaxed mb-6 min-h-[60px] font-serif italic border-l-2 border-amber-900/30 pl-3">
                    {marketItem.type === 'Beast' 
                      ? `Élément: ${marketItem.beast?.element} | Bonus: +${marketItem.beast?.bonus.value} ${marketItem.beast?.bonus.type}`
                      : itemData?.description}
                  </p>
                </div>

                <div className="pt-6 border-t border-amber-900/20 flex items-center justify-between relative z-10">
                  <div className="flex flex-col">
                    <span className="text-[9px] text-amber-700/60 uppercase tracking-widest font-mono mb-1">Prix</span>
                    <div className="flex items-baseline gap-1.5">
                      <span className={`text-xl font-mono ${canAfford ? 'text-amber-400' : 'text-red-400'}`}>
                        {marketItem.cost.toLocaleString()}
                      </span>
                      <span className="text-[10px] text-amber-700/40 uppercase tracking-widest">Pierres</span>
                    </div>
                  </div>
                  
                  <button
                    onClick={() => onBuyItem(marketItem.id)}
                    disabled={!canAfford}
                    className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-bold uppercase tracking-widest transition-all shadow-lg ${
                      canAfford
                        ? 'bg-gradient-to-r from-amber-700 to-amber-600 hover:from-amber-600 hover:to-amber-500 text-white border border-amber-400/20 active:scale-95'
                        : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                    }`}
                  >
                    <ShoppingCart size={16} />
                    <span>Acheter</span>
                  </button>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
};
