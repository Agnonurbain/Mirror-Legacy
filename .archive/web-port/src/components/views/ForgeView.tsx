import React, { useState } from 'react';
import { Clan, Recipe, Item } from '../../types/game';
import { mockForgeRecipes } from '../../data/mockData';
import { Anvil, Hammer, Flame, ChevronRight, Package, AlertCircle, Users, Sparkles, Hexagon } from 'lucide-react';
import { Avatar } from '../ui/Avatar';
import { motion, AnimatePresence } from 'motion/react';

interface ForgeViewProps {
  clan: Clan;
  onForge: (recipeId: string, smithId: string) => void;
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

export const ForgeView: React.FC<ForgeViewProps> = ({ clan, onForge }) => {
  const [craftingRecipeId, setCraftingRecipeId] = useState<string | null>(null);
  const smiths = clan.members.filter(m => m.isAlive && m.currentTask === 'Forge');

  const handleForge = (recipeId: string, smithId: string) => {
    setCraftingRecipeId(recipeId);
    setTimeout(() => {
      onForge(recipeId, smithId);
      setCraftingRecipeId(null);
    }, 1500);
  };

  const canForge = (recipe: Recipe, smithId: string) => {
    const smith = clan.members.find(m => m.id === smithId);
    if (!smith || !smith.skills || smith.skills.forging < recipe.minSkill) return false;

    return recipe.ingredients.every(ing => {
      const item = clan.inventory.find(i => i.id === ing.itemId);
      return item && item.quantity >= ing.quantity;
    });
  };

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="p-4 md:p-8 max-w-[1600px] mx-auto min-h-screen relative"
    >
      {/* Background Texture */}
      <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
      
      {/* Crafting Overlay */}
      <AnimatePresence>
        {craftingRecipeId && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-[200] flex items-center justify-center bg-black/80 backdrop-blur-xl"
          >
            <div className="text-center space-y-8 relative z-10">
              <motion.div 
                animate={{ 
                  scale: [1, 1.2, 1],
                  rotate: [0, 15, -15, 0],
                  filter: ["brightness(1)", "brightness(1.5)", "brightness(1)"]
                }}
                transition={{ duration: 0.8, repeat: Infinity, ease: "easeInOut" }}
                className="w-32 h-32 mx-auto rounded-full bg-gradient-to-br from-orange-900/40 to-black border-2 border-orange-500/50 flex items-center justify-center shadow-[0_0_50px_rgba(249,115,22,0.3)]"
              >
                <Hammer size={48} className="text-orange-400" />
              </motion.div>
              <h3 className="text-4xl font-calligraphy text-orange-500 drop-shadow-[0_0_10px_rgba(249,115,22,0.5)]">Forge en cours...</h3>
              <p className="text-lg opacity-60 italic font-serif text-orange-100/70">"Le marteau frappe, l'artefact s'éveille."</p>
            </div>
            
            {/* Sparks Particles */}
            {[...Array(30)].map((_, i) => (
              <motion.div
                key={i}
                initial={{ x: 0, y: 0, opacity: 0, scale: 0 }}
                animate={{ 
                  x: (Math.random() - 0.5) * 500, 
                  y: (Math.random() - 0.5) * 500,
                  opacity: [0, 1, 0],
                  scale: [0, Math.random() * 2 + 1, 0]
                }}
                transition={{ duration: Math.random() * 1 + 0.5, repeat: Infinity, delay: Math.random() }}
                className="absolute w-2 h-2 bg-orange-400 rounded-full shadow-[0_0_10px_rgba(251,146,60,0.8)]"
                style={{ left: '50%', top: '50%' }}
              />
            ))}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Header Section */}
      <motion.div 
        variants={itemVariants}
        className="bg-[#140d0a] border border-orange-900/30 rounded-3xl p-8 md:p-12 shadow-2xl mb-12 relative overflow-hidden flex flex-col md:flex-row items-center justify-between gap-8"
      >
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMiIgY3k9IjIiIHI9IjEiIGZpbGw9IiM1ZDRkM2QiIGZpbGwtb3BhY2l0eT0iMC4xIi8+PC9zdmc+')] opacity-50" />
        <div className="absolute top-0 right-0 w-64 h-64 bg-orange-900/10 rounded-full blur-3xl pointer-events-none" />
        
        <div className="flex items-center gap-6 relative z-10">
          <div className="w-20 h-20 rounded-full bg-gradient-to-br from-orange-900/40 to-black border border-orange-500/30 flex items-center justify-center shadow-[0_0_20px_rgba(249,115,22,0.2)]">
            <Anvil className="text-orange-500" size={40} />
          </div>
          <div>
            <h2 className="text-4xl md:text-5xl font-calligraphy text-orange-500 drop-shadow-md">Pavillon de la Forge</h2>
            <p className="text-sm text-orange-700/70 font-mono uppercase tracking-[0.2em] mt-2">Création d'Artefacts Spirituels</p>
          </div>
        </div>
        
        <div className="flex flex-col items-center md:items-end relative z-10 bg-black/40 p-6 rounded-2xl border border-orange-900/50 shadow-inner">
          <span className="text-xs text-orange-700/70 uppercase tracking-[0.3em] font-mono mb-2 flex items-center gap-2">
            <Users size={14} className="text-orange-500" />
            Forgerons Actifs
          </span>
          <div className="flex items-baseline gap-2">
            <span className="text-4xl font-mono text-orange-400 drop-shadow-[0_0_10px_rgba(251,146,60,0.5)]">
              {smiths.length}
            </span>
            <span className="text-sm text-orange-700/50 font-serif italic">Disciples</span>
          </div>
        </div>
      </motion.div>

      {smiths.length === 0 ? (
        <motion.div variants={itemVariants} className="bg-[#1a120f] border border-orange-900/30 rounded-3xl p-16 text-center relative overflow-hidden shadow-xl">
          <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay pointer-events-none" />
          <div className="relative z-10 flex flex-col items-center">
            <div className="w-24 h-24 rounded-full bg-black/50 border border-orange-900/50 flex items-center justify-center mb-6">
              <AlertCircle className="w-12 h-12 text-orange-700/30" />
            </div>
            <h3 className="text-3xl font-calligraphy text-orange-500 mb-4">Enclume Silencieuse</h3>
            <p className="text-orange-100/50 font-serif italic text-lg max-w-md">
              Aucun disciple n'est actuellement assigné à la Forge. Assignez cette tâche dans le Registre pour commencer la création.
            </p>
          </div>
        </motion.div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 relative z-10">
          {mockForgeRecipes.map(recipe => (
            <motion.div 
              key={recipe.id} 
              variants={itemVariants}
              className="bg-[#1a120f] border border-orange-900/30 rounded-3xl p-8 shadow-xl relative overflow-hidden group hover:border-orange-500/50 transition-all"
            >
              {/* Decorative Background */}
              <div className="absolute top-0 right-0 w-40 h-40 bg-gradient-to-bl from-orange-900/20 to-transparent rounded-bl-full pointer-events-none opacity-50" />
              <div className="absolute -bottom-10 -right-10 opacity-5 group-hover:opacity-10 transition-opacity duration-500 pointer-events-none">
                <Hexagon size={150} />
              </div>

              <div className="flex items-start justify-between mb-8 relative z-10">
                <div className="flex items-center gap-5">
                  <div className="w-16 h-16 rounded-2xl bg-black/50 border border-orange-900/50 shadow-inner flex items-center justify-center">
                    <Anvil size={32} className="text-orange-400" />
                  </div>
                  <div>
                    <h3 className="text-2xl font-serif font-bold text-orange-100 group-hover:text-orange-400 transition-colors">
                      {recipe.name}
                    </h3>
                    <p className="text-sm text-orange-100/60 font-serif italic mt-1">
                      {recipe.description}
                    </p>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-8 relative z-10">
                {/* Ingredients Section */}
                <div className="space-y-4">
                  <h4 className="text-[10px] uppercase tracking-[0.2em] font-mono text-orange-700/70 flex items-center gap-2">
                    <Package size={12} />
                    Matériaux Requis
                  </h4>
                  <div className="space-y-3">
                    {recipe.ingredients.map(ing => {
                      const inventoryItem = clan.inventory.find(i => i.id === ing.itemId);
                      const hasEnough = (inventoryItem?.quantity || 0) >= ing.quantity;
                      
                      return (
                        <div key={ing.itemId} className="flex items-center justify-between p-3 bg-black/40 rounded-xl border border-orange-900/20">
                          <span className="text-sm font-serif text-orange-100/80">{ing.itemId}</span>
                          <div className="flex items-center gap-2">
                            <span className={`text-xs font-mono font-bold px-2 py-1 rounded bg-black/60 border ${hasEnough ? 'text-orange-400 border-orange-900/50' : 'text-red-400 border-red-900/50'}`}>
                              {inventoryItem?.quantity || 0} / {ing.quantity}
                            </span>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* Smiths Section */}
                <div className="space-y-4">
                  <h4 className="text-[10px] uppercase tracking-[0.2em] font-mono text-orange-700/70 flex items-center gap-2">
                    <Users size={12} />
                    Forgerons Disponibles
                  </h4>
                  <div className="space-y-3 max-h-[200px] overflow-y-auto custom-scrollbar pr-2">
                    {smiths.map(smith => {
                      const canCraftRecipe = canForge(recipe, smith.id);
                      const skillLevel = smith.skills?.forging || 0;
                      
                      return (
                        <div key={smith.id} className="flex items-center justify-between p-3 bg-black/40 rounded-xl border border-orange-900/20">
                          <div className="flex items-center gap-3">
                            <Avatar seed={smith.portraitSeed} name={smith.firstName} realm={smith.realm} size="sm" className="border border-orange-900/50" />
                            <div>
                              <span className="text-sm font-serif font-bold text-orange-100/90 block">
                                {smith.lastName} {smith.firstName}
                              </span>
                              <span className={`text-[10px] font-mono uppercase tracking-widest ${skillLevel >= recipe.minSkill ? 'text-orange-500' : 'text-red-400'}`}>
                                Niv. {skillLevel} / {recipe.minSkill}
                              </span>
                            </div>
                          </div>
                          <button
                            onClick={() => handleForge(recipe.id, smith.id)}
                            disabled={!canCraftRecipe || craftingRecipeId !== null}
                            className={`p-2 rounded-lg transition-all ${
                              canCraftRecipe && craftingRecipeId === null
                                ? 'bg-orange-900/30 text-orange-400 hover:bg-orange-500 hover:text-white border border-orange-500/30'
                                : 'bg-stone-900/50 text-stone-600 border border-stone-800 cursor-not-allowed'
                            }`}
                            title={!canCraftRecipe ? "Matériaux ou niveau insuffisant" : "Forger"}
                          >
                            <Flame size={16} />
                          </button>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      )}
    </motion.div>
  );
};
