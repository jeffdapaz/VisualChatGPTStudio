# Copilot Instructions

## General Guidelines
- When making changes, do not modify functionalities that already work/tested; implement only the specific requested change, with the smallest possible impact.
- Always write code in English.
- Make implementations step-by-step, and be detailed in your response, never assuming the user will understand everything.
- If you have doubts, ask the user for clarification before implementing.
- When creating new classes, add the following standard regions in the order below, and always close them with "endregion": 
	- Constants 
	- Properties 
	- Constructors 
	- Public Methods 
	- Private Methods
- When creating methods, respect the regions already present in the file when they exist. For example, public methods should be placed in the public methods region, private methods in the private methods region, etc.
- When creating methods, properties, etc., always add comments using the C# "<summary>" pattern.