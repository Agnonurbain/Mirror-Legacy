import React from 'react';
import { Sparkles, Users, Building2, Globe, Swords, Play, X } from 'lucide-react';

interface TutorialModalProps {
  onClose: () => void;
}

export const TutorialModal: React.FC<TutorialModalProps> = ({ onClose }) => {
  const steps = [
    {
      title: "Bienvenue Patriarche",
      description: "Vous êtes à la tête d'un clan de cultivateurs naissant. Votre but est d'atteindre l'immortalité et de dominer le monde spirituel.",
      icon: Sparkles,
      color: "text-gold-500"
    },
    {
      title: "Gérez vos Membres",
      description: "Assignez des tâches à vos membres : Culture pour progresser, Récolte pour les ressources, ou Alchimie pour créer des pilules.",
      icon: Users,
      color: "text-blue-400"
    },
    {
      title: "Développez le Clan",
      description: "Améliorez vos infrastructures pour obtenir des bonus permanents et augmenter le prestige de votre clan.",
      icon: Building2,
      color: "text-jade-400"
    },
    {
      title: "Explorez le Monde",
      description: "Interagissez avec les autres sectes et lancez des expéditions périlleuses pour trouver des trésors légendaires.",
      icon: Globe,
      color: "text-purple-400"
    }
  ];

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center p-4 bg-zinc-950/80 backdrop-blur-sm animate-in fade-in duration-300">
      <div className="bg-zinc-900 border border-zinc-800 rounded-3xl max-w-2xl w-full overflow-hidden shadow-2xl relative">
        <button 
          onClick={onClose}
          className="absolute top-6 right-6 text-zinc-500 hover:text-zinc-300 transition-colors"
        >
          <X size={24} />
        </button>

        <div className="p-10 space-y-8">
          <div className="text-center space-y-2">
            <h2 className="text-3xl font-serif font-bold text-zinc-100">Commencez votre Ascension</h2>
            <p className="text-zinc-500">Guide rapide pour le nouveau Patriarche</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {steps.map((step, idx) => {
              const Icon = step.icon;
              return (
                <div key={idx} className="flex gap-4 p-4 rounded-2xl bg-zinc-800/30 border border-zinc-800/50">
                  <div className={`mt-1 ${step.color}`}>
                    <Icon size={24} />
                  </div>
                  <div className="space-y-1">
                    <h3 className="font-serif font-bold text-zinc-200">{step.title}</h3>
                    <p className="text-xs text-zinc-500 leading-relaxed">{step.description}</p>
                  </div>
                </div>
              );
            })}
          </div>

          <div className="pt-4">
            <button 
              onClick={onClose}
              className="w-full bg-gold-600 hover:bg-gold-500 text-zinc-950 font-bold py-4 rounded-2xl transition-all flex items-center justify-center gap-2 group"
            >
              Compris, que l'ascension commence !
              <Play size={18} className="group-hover:translate-x-1 transition-transform" fill="currentColor" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
