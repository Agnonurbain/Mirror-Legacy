#!/usr/bin/env python3
"""
Parseur de logs Unity pour Claude Code
Extraction structurée des erreurs, warnings et infos
"""

import sys
import re
import json
from pathlib import Path
from datetime import datetime

def parse_unity_log(log_file):
    """Parse un fichier log Unity et extrait les informations structurées"""
    
    errors = []
    warnings = []
    infos = []
    compilation_errors = []
    
    patterns = {
        'error': re.compile(r'^Error:\s*(.+)$', re.IGNORECASE | re.MULTILINE),
        'warning': re.compile(r'^Warning:\s*(.+)$', re.IGNORECASE | re.MULTILINE),
        'cs_error': re.compile(r'error\s+CS(\d+):\s*(.+?)(?:\s+at|$)', re.IGNORECASE),
        'exception': re.compile(r'([^:]+Exception):\s*(.+?)(?:\n\s+at|$)', re.DOTALL),
    }
    
    try:
        with open(log_file, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
            lines = content.split('\n')
            
            for i, line in enumerate(lines):
                # Erreurs de compilation C#
                if 'error CS' in line.lower():
                    match = patterns['cs_error'].search(line)
                    if match:
                        compilation_errors.append({
                            'code': match.group(1),
                            'message': match.group(2).strip(),
                            'line_number': i + 1
                        })
                
                # Warnings
                elif 'warning' in line.lower() and 'cs' in line.lower():
                    warnings.append({
                        'message': line.strip(),
                        'line_number': i + 1
                    })
                
                # Exceptions
                elif 'Exception' in line:
                    match = patterns['exception'].search(line)
                    if match:
                        errors.append({
                            'type': match.group(1),
                            'message': match.group(2).strip(),
                            'line_number': i + 1
                        })
                        
    except Exception as e:
        return {
            'status': 'error',
            'message': f'Impossible de lire le log: {str(e)}',
            'timestamp': datetime.utcnow().isoformat()
        }
    
    # Résumé
    result = {
        'status': 'error' if (errors or compilation_errors) else 'success',
        'timestamp': datetime.utcnow().isoformat(),
        'summary': {
            'total_errors': len(errors) + len(compilation_errors),
            'total_warnings': len(warnings),
            'compilation_errors': len(compilation_errors),
            'runtime_errors': len(errors)
        },
        'compilation_errors': compilation_errors,
        'warnings': warnings[:20],  # Limiter à 20 warnings
        'errors': errors[:20],  # Limiter à 20 erreurs
    }
    
    return result

def main():
    if len(sys.argv) > 1:
        log_file = sys.argv[1]
    else:
        # Chemins par défaut pour Ubuntu
        possible_paths = [
            Path.home() / ".config/unity3d/Unity/Editor.log",
            Path.home() / ".cache/unity3d/Unity/Editor.log",
            "Editor.log"
        ]
        
        log_file = None
        for path in possible_paths:
            if path.exists():
                log_file = str(path)
                break
        
        if not log_file:
            print(json.dumps({
                'status': 'error',
                'message': 'Log Unity non trouvé. Spécifiez le chemin en argument'
            }, indent=2))
            sys.exit(1)
    
    result = parse_unity_log(log_file)
    print(json.dumps(result, indent=2, ensure_ascii=False))
    
    # Code de sortie
    sys.exit(0 if result['status'] == 'success' else 1)

if __name__ == "__main__":
    main()