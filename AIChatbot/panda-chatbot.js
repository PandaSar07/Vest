// panda-chatbot-embed.js
(function () {
    // Configuration
    const CONFIG = {
        position: 'right',
        primaryColor: '#2d3e2d',
        welcomeMessage: '🐼 *waves paw* Hello! I\'m Panda, your friendly trading research assistant. How can I help you navigate the platform today? This is not financial advice.',
        botName: 'Panda Assistant'
    };

    // Inject styles with Panda theme
    const styles = `
        .panda-chatbot-container * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        }
        
        .panda-chatbot-toggle {
            position: fixed;
            bottom: 20px;
            ${CONFIG.position}: 20px;
            width: 60px;
            height: 60px;
            border-radius: 30px;
            background: ${CONFIG.primaryColor};
            color: white;
            border: none;
            cursor: pointer;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 28px;
            transition: transform 0.2s;
            z-index: 10000;
            border: 3px solid #a5d6a5;
        }
        
        .panda-chatbot-toggle:hover {
            transform: scale(1.05);
        }
        
        .panda-chatbot-window {
            position: fixed;
            bottom: 90px;
            ${CONFIG.position}: 20px;
            width: 380px;
            height: 600px;
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            display: none;
            flex-direction: column;
            overflow: hidden;
            z-index: 10000;
        }
        
        .panda-chatbot-window.open {
            display: flex;
        }
        
        .panda-chatbot-header {
            background: ${CONFIG.primaryColor};
            color: white;
            padding: 16px;
            display: flex;
            align-items: center;
            gap: 12px;
        }
        
        .panda-header-avatar {
            width: 40px;
            height: 40px;
            background: white;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 24px;
            border: 2px solid #a5d6a5;
        }
        
        .panda-header-text {
            flex: 1;
        }
        
        .panda-header-text h3 {
            font-size: 1rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 6px;
        }
        
        .panda-header-text h3 span {
            background: rgba(255,255,255,0.2);
            padding: 2px 6px;
            border-radius: 12px;
            font-size: 0.6rem;
            font-weight: normal;
        }
        
        .panda-chatbot-close {
            background: none;
            border: none;
            color: white;
            cursor: pointer;
            font-size: 20px;
            opacity: 0.8;
        }
        
        .panda-chatbot-messages {
            flex: 1;
            overflow-y: auto;
            padding: 16px;
            background: #f8fafc;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }
        
        .panda-chatbot-message {
            display: flex;
            max-width: 85%;
            align-items: flex-end;
            gap: 8px;
        }
        
        .panda-chatbot-message.user {
            align-self: flex-end;
        }
        
        .panda-chatbot-message.bot {
            align-self: flex-start;
        }
        
        .panda-message-avatar {
            width: 28px;
            height: 28px;
            background: ${CONFIG.primaryColor};
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 16px;
            flex-shrink: 0;
            border: 2px solid #a5d6a5;
        }
        
        .panda-message-bubble {
            padding: 10px 14px;
            border-radius: 16px;
            font-size: 0.9rem;
            line-height: 1.4;
        }
        
        .user .panda-message-bubble {
            background: ${CONFIG.primaryColor};
            color: white;
            border-bottom-right-radius: 4px;
        }
        
        .bot .panda-message-bubble {
            background: white;
            color: #1e293b;
            border: 1px solid #e2e8f0;
            border-bottom-left-radius: 4px;
        }
        
        .panda-chatbot-input-area {
            padding: 16px;
            background: white;
            border-top: 1px solid #eef2f6;
            display: flex;
            gap: 8px;
        }
        
        .panda-chatbot-input {
            flex: 1;
            padding: 10px 14px;
            border: 1px solid #e2e8f0;
            border-radius: 24px;
            outline: none;
            font-size: 0.9rem;
        }
        
        .panda-chatbot-input:focus {
            border-color: ${CONFIG.primaryColor};
        }
        
        .panda-chatbot-send {
            background: ${CONFIG.primaryColor};
            color: white;
            border: none;
            width: 40px;
            height: 40px;
            border-radius: 20px;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 18px;
        }
        
        .panda-chatbot-footer {
            padding: 8px;
            background: #f1f5f9;
            text-align: center;
            font-size: 0.65rem;
            color: #64748b;
            font-weight: 500;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 5px;
        }
        
        .panda-bamboo {
            color: #a5d6a5;
        }
    `;

    // Inject styles
    const styleSheet = document.createElement('style');
    styleSheet.textContent = styles;
    document.head.appendChild(styleSheet);

    // Create chatbot HTML with Panda theme
    const chatbotHTML = `
        <div class="panda-chatbot-container">
            <button class="panda-chatbot-toggle" id="pandaChatbotToggle">🐼</button>
            
            <div class="panda-chatbot-window" id="pandaChatbotWindow">
                <div class="panda-chatbot-header">
                    <div class="panda-header-avatar">🐼</div>
                    <div class="panda-header-text">
                        <h3>Panda Assistant <span>v1.0</span></h3>
                        <div style="font-size: 0.7rem; opacity: 0.9;">Your friendly research guide</div>
                    </div>
                    <button class="panda-chatbot-close" id="pandaChatbotClose">✕</button>
                </div>
                
                <div class="panda-chatbot-messages" id="pandaChatbotMessages">
                    <div class="panda-chatbot-message bot">
                        <div class="panda-message-avatar">🐼</div>
                        <div class="panda-message-bubble">${CONFIG.welcomeMessage}</div>
                    </div>
                </div>
                
                <div class="panda-chatbot-input-area">
                    <input type="text" class="panda-chatbot-input" id="pandaChatbotInput" placeholder="Ask Panda...">
                    <button class="panda-chatbot-send" id="pandaChatbotSend">📤</button>
                </div>
                
                <div class="panda-chatbot-footer">
                    <span>⚠️ THIS IS NOT FINANCIAL ADVICE</span>
                    <span class="panda-bamboo">🎋</span>
                </div>
            </div>
        </div>
    `;

    // Append to body
    document.body.insertAdjacentHTML('beforeend', chatbotHTML);

    // Get elements
    const toggle = document.getElementById('pandaChatbotToggle');
    const window = document.getElementById('pandaChatbotWindow');
    const close = document.getElementById('pandaChatbotClose');
    const input = document.getElementById('pandaChatbotInput');
    const send = document.getElementById('pandaChatbotSend');
    const messages = document.getElementById('pandaChatbotMessages');

    // Toggle chatbot
    toggle.addEventListener('click', () => {
        window.classList.add('open');
        toggle.style.display = 'none';
    });

    close.addEventListener('click', () => {
        window.classList.remove('open');
        toggle.style.display = 'flex';
    });

    // Send message function with Panda responses
    function sendMessage() {
        const text = input.value.trim();
        if (!text) return;

        // Add user message
        const userMsg = document.createElement('div');
        userMsg.className = 'panda-chatbot-message user';
        userMsg.innerHTML = `<div class="panda-message-bubble">${escapeHtml(text)}</div>`;
        messages.appendChild(userMsg);

        input.value = '';
        messages.scrollTop = messages.scrollHeight;

        // Simulate Panda response
        setTimeout(() => {
            const botMsg = document.createElement('div');
            botMsg.className = 'panda-chatbot-message bot';

            // Simple response logic
            let response = "🐼 *tilts head* Thanks for asking! I can help you navigate our platform. Try asking about hypotheses, backtesting, or specific features. This is not financial advice.";

            if (text.toLowerCase().includes('hypothesis')) {
                response = "🐼 To create a hypothesis, go to the Research Lab and click 'New Hypothesis'. Start with a clear question! This is not financial advice.";
            } else if (text.toLowerCase().includes('backtest')) {
                response = "🐼 Head to the Backtesting Engine, select your hypothesis, and run the experiment. Check the Context Log for market conditions! This is not financial advice.";
            } else if (text.toLowerCase().includes('panda')) {
                response = "🐼 *happily munches bamboo* That's me! I'm here to guide you through the platform. What would you like to know? This is not financial advice.";
            }

            botMsg.innerHTML = `
                <div class="panda-message-avatar">🐼</div>
                <div class="panda-message-bubble">${response}</div>
            `;
            messages.appendChild(botMsg);
            messages.scrollTop = messages.scrollHeight;
        }, 600);
    }

    // Event listeners
    send.addEventListener('click', sendMessage);
    input.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            sendMessage();
        }
    });

    // Escape HTML
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();