import React from 'react';

interface AvatarProps {
  seed: string;
  name: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
  realm?: string;
}

export const Avatar: React.FC<AvatarProps> = ({ seed, name, size = 'md', className = '', realm }) => {
  const sizeClasses = {
    sm: 'w-10 h-10',
    md: 'w-14 h-14',
    lg: 'w-20 h-20',
    xl: 'w-32 h-32'
  };

  const getRealmColor = (realm?: string) => {
    if (!realm) return 'border-paper/10';
    if (realm.includes('Immortel')) return 'border-gold-500 shadow-[0_0_15px_rgba(212,175,55,0.4)]';
    if (realm.includes('Noyau')) return 'border-purple-500 shadow-[0_0_10px_rgba(168,85,247,0.3)]';
    if (realm.includes('Fondation')) return 'border-blue-500 shadow-[0_0_10px_rgba(59,130,246,0.3)]';
    if (realm.includes('Qi')) return 'border-jade-500 shadow-[0_0_10px_rgba(16,185,129,0.3)]';
    return 'border-paper/20';
  };

  return (
    <div className={`relative group/avatar ${sizeClasses[size]} ${className}`}>
      {/* Decorative Frame */}
      <div className={`absolute inset-0 rounded-2xl border-2 ${getRealmColor(realm)} transition-all duration-500 group-hover/avatar:scale-110 z-10`} />
      <div className="absolute -inset-1 rounded-2xl border border-gold-500/10 opacity-0 group-hover/avatar:opacity-100 transition-opacity animate-pulse pointer-events-none" />
      
      {/* Ink Splash Background */}
      <div className="absolute inset-1 bg-ink rounded-xl overflow-hidden">
        <div className="absolute inset-0 opacity-20 bg-[radial-gradient(circle_at_center,_var(--tw-gradient-stops))] from-gold-500/20 via-transparent to-transparent animate-qi-flow" />
        
        <img 
          src={`https://api.dicebear.com/9.x/adventurer-neutral/svg?seed=${encodeURIComponent(name)}&backgroundColor=transparent`} 
          alt={name}
          className="w-full h-full object-cover relative z-0 painting-effect"
          referrerPolicy="no-referrer"
          loading="lazy"
        />
      </div>

      {/* Realm Badge (if provided and size is large enough) */}
      {realm && (size === 'lg' || size === 'xl') && (
        <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 z-20 whitespace-nowrap">
          <span className="px-3 py-1 bg-ink border border-gold-500/30 rounded-full text-[8px] uppercase tracking-[0.2em] text-gold-500 shadow-xl font-bold">
            {realm}
          </span>
        </div>
      )}
    </div>
  );
};
